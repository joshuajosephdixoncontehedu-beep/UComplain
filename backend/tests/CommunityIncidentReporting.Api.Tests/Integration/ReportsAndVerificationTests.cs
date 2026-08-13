using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using CommunityIncidentReporting.Application.Features.Verification.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;

namespace CommunityIncidentReporting.Api.Tests.Integration;

public class ReportsAndVerificationTests : IAsyncLifetime
{
    // Matches the server's AddJsonOptions(JsonStringEnumConverter) in Program.cs — the
    // default System.Net.Http.Json options know nothing about that converter, so
    // reading an enum-as-string response with the default options throws.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private const string Password = "Correct-Horse-Battery-Staple-1";

    private Guid _incidentManagerId;
    private Guid _categoryId;
    private Guid _reporterId;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        await using var db = _factory.CreateDbContext();

        var superAdmin = NewAdmin("superadmin@cirs.gov.sl", AdminRole.SuperAdmin);
        var incidentManager = NewAdmin("manager@cirs.gov.sl", AdminRole.IncidentManager);
        var reviewer = NewAdmin("reviewer@cirs.gov.sl", AdminRole.Reviewer);
        var analyst = NewAdmin("analyst@cirs.gov.sl", AdminRole.ReadOnlyAnalyst);
        db.AdminUsers.AddRange(superAdmin, incidentManager, reviewer, analyst);
        _incidentManagerId = incidentManager.Id;

        var category = new IncidentCategory
        {
            Id = Guid.NewGuid(),
            Name = "Flooding",
            Description = "Flood-related reports.",
            DefaultPriority = IncidentPriority.High,
            SlaHours = 12,
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.IncidentCategories.Add(category);
        _categoryId = category.Id;

        var reporter = new Reporter
        {
            Id = Guid.NewGuid(),
            WhatsAppNumberHash = "hash-1",
            MaskedContactReference = "+232 76 *** 111",
            VerificationStatus = VerificationStatus.Verified,
            IsRestricted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Reporters.Add(reporter);
        _reporterId = reporter.Id;

        // One Verified report (belongs in the operational queue) and one Pending
        // report (must NOT appear there) — the core Phase 3 business rule under test.
        db.IncidentReports.Add(NewReport(category.Id, reporter.Id, VerificationStatus.Verified, CaseStatus.UnderReview));
        db.IncidentReports.Add(NewReport(category.Id, reporter.Id, VerificationStatus.Pending, CaseStatus.VerificationPending));

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static AdminUser NewAdmin(string email, AdminRole role) => new()
    {
        Id = Guid.NewGuid(),
        FullName = $"Test {role}",
        Email = email,
        PasswordHash = new BCryptPasswordHasher().Hash(Password),
        Role = role,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static IncidentReport NewReport(
        Guid categoryId, Guid reporterId, VerificationStatus verificationStatus, CaseStatus caseStatus)
    {
        var now = DateTimeOffset.UtcNow;
        return new IncidentReport
        {
            Id = Guid.NewGuid(),
            CaseReference = $"CIRS-TEST-{Guid.NewGuid():N}"[..20],
            CategoryId = categoryId,
            ReporterId = reporterId,
            SourceChannel = SourceChannel.WhatsApp,
            Description = "Test report description that is long enough to be realistic.",
            IncidentOccurredAt = now.AddHours(-2),
            LocationDescription = "Kroo Bay, Freetown",
            Priority = IncidentPriority.High,
            VerificationStatus = verificationStatus,
            CaseStatus = caseStatus,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private async Task<string> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", new LoginRequest(email, Password));
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        return tokens!.AccessToken;
    }

    private async Task AuthenticateAsAsync(string email) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(email));

    [Fact]
    public async Task GetReports_OnlyReturnsVerifiedReports()
    {
        await AuthenticateAsAsync("analyst@cirs.gov.sl");

        var response = await _client.GetAsync("/api/admin/reports");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<IncidentReportListItemDto>>(JsonOptions);
        page!.Total.Should().Be(1);
        page.Items.Single().VerificationStatus.Should().Be(VerificationStatus.Verified);
    }

    [Fact]
    public async Task VerificationQueue_ContainsThePendingReportButNotTheVerifiedOne()
    {
        await AuthenticateAsAsync("reviewer@cirs.gov.sl");

        var response = await _client.GetAsync("/api/admin/verification-queue");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var queue = await response.Content.ReadFromJsonAsync<VerificationQueueResponse>(JsonOptions);
        queue!.Pending.Should().HaveCount(1);
        queue.NeedsClarification.Should().BeEmpty();
    }

    [Fact]
    public async Task ApprovingAVerificationDecision_MovesTheReportIntoTheOperationalQueue()
    {
        await AuthenticateAsAsync("reviewer@cirs.gov.sl");
        var queueResponse = await _client.GetAsync("/api/admin/verification-queue");
        var queue = await queueResponse.Content.ReadFromJsonAsync<VerificationQueueResponse>(JsonOptions);
        var pendingReportId = queue!.Pending.Single().Id;

        var decision = await _client.PostAsJsonAsync(
            $"/api/admin/reports/{pendingReportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));
        decision.StatusCode.Should().Be(HttpStatusCode.OK);

        var reportsResponse = await _client.GetAsync("/api/admin/reports");
        var page = await reportsResponse.Content.ReadFromJsonAsync<PagedResult<IncidentReportListItemDto>>(JsonOptions);
        page!.Total.Should().Be(2, "the newly-approved report should now join the already-verified one");
    }

    [Fact]
    public async Task RejectingAVerificationDecision_RequiresAReasonAndNeverAppearsInReports()
    {
        await AuthenticateAsAsync("reviewer@cirs.gov.sl");
        var queueResponse = await _client.GetAsync("/api/admin/verification-queue");
        var queue = await queueResponse.Content.ReadFromJsonAsync<VerificationQueueResponse>(JsonOptions);
        var pendingReportId = queue!.Pending.Single().Id;

        var withoutReason = await _client.PostAsJsonAsync(
            $"/api/admin/reports/{pendingReportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Reject, null));
        withoutReason.StatusCode.Should().Be((HttpStatusCode)422, "a reason is required to reject a report");

        var withReason = await _client.PostAsJsonAsync(
            $"/api/admin/reports/{pendingReportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Reject, "Not enough detail to act on."));
        withReason.StatusCode.Should().Be(HttpStatusCode.OK);

        var reportsResponse = await _client.GetAsync("/api/admin/reports");
        var page = await reportsResponse.Content.ReadFromJsonAsync<PagedResult<IncidentReportListItemDto>>(JsonOptions);
        page!.Total.Should().Be(1, "a rejected report must never enter the operational queue");
    }

    [Fact]
    public async Task DecidingOnAnAlreadyVerifiedReport_IsRejectedAsABusinessRuleViolation()
    {
        await AuthenticateAsAsync("reviewer@cirs.gov.sl");
        var reportsResponse = await _client.GetAsync("/api/admin/reports");
        var page = await reportsResponse.Content.ReadFromJsonAsync<PagedResult<IncidentReportListItemDto>>(JsonOptions);
        var verifiedReportId = page!.Items.Single().Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/admin/reports/{verifiedReportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ChangingStatus_ToAnIllegalTargetStatus_Returns409()
    {
        await AuthenticateAsAsync("manager@cirs.gov.sl");
        var reportsResponse = await _client.GetAsync("/api/admin/reports");
        var page = await reportsResponse.Content.ReadFromJsonAsync<PagedResult<IncidentReportListItemDto>>(JsonOptions);
        var reportId = page!.Items.Single().Id; // currently UnderReview

        // UnderReview -> Resolved is not an allowed direct transition.
        var response = await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/status", new ChangeStatusRequest(CaseStatus.Resolved, null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ChangingStatus_ToALegalTargetStatus_Succeeds()
    {
        await AuthenticateAsAsync("manager@cirs.gov.sl");
        var reportsResponse = await _client.GetAsync("/api/admin/reports");
        var page = await reportsResponse.Content.ReadFromJsonAsync<PagedResult<IncidentReportListItemDto>>(JsonOptions);
        var reportId = page!.Items.Single().Id; // currently UnderReview

        var response = await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/status", new ChangeStatusRequest(CaseStatus.InProgress, "Starting work."));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<IncidentReportDetailDto>(JsonOptions);
        detail!.CaseStatus.Should().Be(CaseStatus.InProgress);
        detail.StatusHistory.Should().ContainSingle();
    }

    [Fact]
    public async Task AssigningAReport_WritesAnAssignmentAndAutoAdvancesUnderReviewToAssigned()
    {
        await AuthenticateAsAsync("manager@cirs.gov.sl");
        var reportsResponse = await _client.GetAsync("/api/admin/reports");
        var page = await reportsResponse.Content.ReadFromJsonAsync<PagedResult<IncidentReportListItemDto>>(JsonOptions);
        var reportId = page!.Items.Single().Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/assign", new AssignReportRequest(_incidentManagerId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<IncidentReportDetailDto>(JsonOptions);
        detail!.CaseStatus.Should().Be(CaseStatus.Assigned);
        detail.AssignedAdminId.Should().Be(_incidentManagerId);
        detail.Assignments.Should().ContainSingle();
    }

    [Fact]
    public async Task ReadOnlyAnalyst_CannotAssignReports()
    {
        await AuthenticateAsAsync("analyst@cirs.gov.sl");
        var reportsResponse = await _client.GetAsync("/api/admin/reports");
        var page = await reportsResponse.Content.ReadFromJsonAsync<PagedResult<IncidentReportListItemDto>>(JsonOptions);
        var reportId = page!.Items.Single().Id;

        var response = await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/assign", new AssignReportRequest(_incidentManagerId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivatingTheLastActiveSuperAdmin_IsRejected()
    {
        await AuthenticateAsAsync("superadmin@cirs.gov.sl");

        await using var db = _factory.CreateDbContext();
        var superAdminId = db.AdminUsers.Single(a => a.Role == AdminRole.SuperAdmin).Id;

        var response = await _client.PostAsync($"/api/admin/administrators/{superAdminId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReadOnlyAnalyst_CannotAccessAdministrators()
    {
        await AuthenticateAsAsync("analyst@cirs.gov.sl");

        var response = await _client.GetAsync("/api/admin/administrators");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
