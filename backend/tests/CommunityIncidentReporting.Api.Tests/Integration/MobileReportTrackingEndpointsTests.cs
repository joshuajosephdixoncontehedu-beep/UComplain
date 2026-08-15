using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Api.Tests.Integration;

public class MobileReportTrackingEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _categoryId;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        await using var db = _factory.CreateDbContext();
        var category = new IncidentCategory
        {
            Id = Guid.NewGuid(),
            Name = "Road Hazard",
            Description = "Potholes, debris, damaged signage.",
            DefaultPriority = IncidentPriority.Medium,
            SlaHours = 48,
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.IncidentCategories.Add(category);
        await db.SaveChangesAsync();
        _categoryId = category.Id;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<string> RegisterReporterAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/mobile/auth/register", new RegisterReporterRequest(
            "Reporter Test", email, "+23276111999", "Correct-Horse-1", "Correct-Horse-1", true));

        var message = _factory.EmailService.LatestFor(email);
        var code = RecordingEmailService.ExtractOtpCode(message);

        var verify = await _client.PostAsJsonAsync("/api/mobile/auth/verify-email-otp", new VerifyEmailOtpRequest(email, code));
        var tokens = await verify.Content.ReadFromJsonAsync<ReporterAuthTokenResponse>();
        return tokens!.AccessToken;
    }

    private void Authorize(string accessToken) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    private async Task<Guid> CreateReportAsync(string description)
    {
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, description, DateTimeOffset.UtcNow.AddHours(-1), "Somewhere", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);
        return report!.Id;
    }

    private async Task SetReportStatusAsync(Guid reportId, VerificationStatus verificationStatus, CaseStatus caseStatus)
    {
        await using var db = _factory.CreateDbContext();
        var report = await db.IncidentReports.SingleAsync(r => r.Id == reportId);
        report.VerificationStatus = verificationStatus;
        report.CaseStatus = caseStatus;
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedAdminAsync()
    {
        await using var db = _factory.CreateDbContext();
        var admin = new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = "Reviewer Admin",
            Email = $"reviewer-{Guid.NewGuid():N}@cirs.gov.sl",
            PasswordHash = new BCryptPasswordHasher().Hash("Correct-Horse-1"),
            Role = AdminRole.Reviewer,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();
        return admin.Id;
    }

    [Fact]
    public async Task GetMyReports_FiltersByStatusBucket()
    {
        Authorize(await RegisterReporterAsync("tracker@example.com"));

        var activeId = await CreateReportAsync("Still awaiting verification.");
        var resolvedId = await CreateReportAsync("Already fixed.");
        await SetReportStatusAsync(resolvedId, VerificationStatus.Verified, CaseStatus.Resolved);
        var rejectedId = await CreateReportAsync("Turned out to be spam.");
        await SetReportStatusAsync(rejectedId, VerificationStatus.Rejected, CaseStatus.Rejected);

        var all = await _client.GetAsync("/api/mobile/reports");
        var allPage = await all.Content.ReadFromJsonAsync<PagedResult<MobileReportListItemDto>>(JsonOptions);
        allPage!.Total.Should().Be(3);

        var active = await _client.GetAsync("/api/mobile/reports?status=Active");
        var activePage = await active.Content.ReadFromJsonAsync<PagedResult<MobileReportListItemDto>>(JsonOptions);
        activePage!.Items.Should().ContainSingle(i => i.Id == activeId);
        activePage.Items[0].StatusBadge.Should().Be("Awaiting verification");
        activePage.Items[0].TrackerStage.Should().Be(1);

        var resolved = await _client.GetAsync("/api/mobile/reports?status=Resolved");
        var resolvedPage = await resolved.Content.ReadFromJsonAsync<PagedResult<MobileReportListItemDto>>(JsonOptions);
        resolvedPage!.Items.Should().ContainSingle(i => i.Id == resolvedId);
        resolvedPage.Items[0].ProgressPercent.Should().Be(100);

        var rejected = await _client.GetAsync("/api/mobile/reports?status=Rejected");
        var rejectedPage = await rejected.Content.ReadFromJsonAsync<PagedResult<MobileReportListItemDto>>(JsonOptions);
        rejectedPage!.Items.Should().ContainSingle(i => i.Id == rejectedId);
        rejectedPage.Items[0].TrackerStage.Should().BeNull();
    }

    [Fact]
    public async Task GetCounts_ReturnsCountsPerBucket()
    {
        Authorize(await RegisterReporterAsync("counts@example.com"));

        await CreateReportAsync("Active one.");
        var resolvedId = await CreateReportAsync("Resolved one.");
        await SetReportStatusAsync(resolvedId, VerificationStatus.Verified, CaseStatus.Resolved);

        var response = await _client.GetAsync("/api/mobile/reports/counts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var counts = await response.Content.ReadFromJsonAsync<ReportCountsDto>(JsonOptions);
        counts!.Active.Should().Be(1);
        counts.Resolved.Should().Be(1);
        counts.Rejected.Should().Be(0);
        counts.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetTimeline_ReturnsHistoryInChronologicalOrder()
    {
        Authorize(await RegisterReporterAsync("timeline@example.com"));
        var reportId = await CreateReportAsync("Report with a history.");
        var adminId = await SeedAdminAsync();

        await using (var db = _factory.CreateDbContext())
        {
            var now = DateTimeOffset.UtcNow;
            db.StatusHistories.AddRange(
                new StatusHistory
                {
                    Id = Guid.NewGuid(), IncidentReportId = reportId, PreviousStatus = CaseStatus.VerificationPending,
                    NewStatus = CaseStatus.UnderReview, ChangedByAdminId = adminId, CreatedAt = now.AddMinutes(-10)
                },
                new StatusHistory
                {
                    Id = Guid.NewGuid(), IncidentReportId = reportId, PreviousStatus = CaseStatus.UnderReview,
                    NewStatus = CaseStatus.Assigned, ChangedByAdminId = adminId, CreatedAt = now
                });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/mobile/reports/{reportId}/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = await response.Content.ReadFromJsonAsync<List<MobileReportStatusHistoryDto>>(JsonOptions);
        timeline.Should().HaveCount(2);
        timeline![0].NewStatus.Should().Be(CaseStatus.UnderReview);
        timeline[1].NewStatus.Should().Be(CaseStatus.Assigned);
    }

    [Fact]
    public async Task AddInformation_ThenGetInformation_RoundTrips()
    {
        Authorize(await RegisterReporterAsync("addinfo@example.com"));
        var reportId = await CreateReportAsync("Report needing a follow-up note.");

        var add = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/{reportId}/information", new AddReportInformationRequest("It's gotten worse overnight.", null));

        add.StatusCode.Should().Be(HttpStatusCode.OK);
        var added = await add.Content.ReadFromJsonAsync<ReportInformationDto>(JsonOptions);
        added!.Message.Should().Be("It's gotten worse overnight.");

        var list = await _client.GetAsync($"/api/mobile/reports/{reportId}/information");
        var items = await list.Content.ReadFromJsonAsync<List<ReportInformationDto>>(JsonOptions);
        items.Should().ContainSingle(i => i.Id == added.Id);
    }

    [Fact]
    public async Task AddInformation_WithAttachmentNotOnTheReport_Returns404()
    {
        Authorize(await RegisterReporterAsync("badattachment@example.com"));
        var reportId = await CreateReportAsync("Report without any attachments.");

        var add = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/{reportId}/information",
            new AddReportInformationRequest("Here's a photo.", Guid.NewGuid()));

        add.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddInformation_WhenReportIsNoLongerActive_Returns409()
    {
        Authorize(await RegisterReporterAsync("resolvedinfo@example.com"));
        var reportId = await CreateReportAsync("Report already resolved.");
        await SetReportStatusAsync(reportId, VerificationStatus.Verified, CaseStatus.Resolved);

        var add = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/{reportId}/information", new AddReportInformationRequest("Too late.", null));

        add.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Withdraw_FromVerificationPending_SetsWithdrawnFields()
    {
        Authorize(await RegisterReporterAsync("withdrawer@example.com"));
        var reportId = await CreateReportAsync("Report I want to withdraw.");

        var withdraw = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/{reportId}/withdraw", new WithdrawReportRequest("Resolved this myself."));

        withdraw.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await withdraw.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);
        report!.CaseStatus.Should().Be(CaseStatus.Withdrawn);
        report.WithdrawnAt.Should().NotBeNull();
        report.WithdrawalReason.Should().Be("Resolved this myself.");

        var listed = await _client.GetAsync("/api/mobile/reports?status=Active");
        var page = await listed.Content.ReadFromJsonAsync<PagedResult<MobileReportListItemDto>>(JsonOptions);
        page!.Items.Should().BeEmpty("a withdrawn report is no longer Active");
    }

    [Fact]
    public async Task Withdraw_WhenAlreadyInProgress_Returns409()
    {
        Authorize(await RegisterReporterAsync("toolate@example.com"));
        var reportId = await CreateReportAsync("Report already being worked.");
        await SetReportStatusAsync(reportId, VerificationStatus.Verified, CaseStatus.InProgress);

        var withdraw = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/{reportId}/withdraw", new WithdrawReportRequest("Changed my mind."));

        withdraw.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Withdraw_ByNonOwner_Returns404()
    {
        Authorize(await RegisterReporterAsync("realowner@example.com"));
        var reportId = await CreateReportAsync("Someone else's report.");

        Authorize(await RegisterReporterAsync("intruder@example.com"));
        var withdraw = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/{reportId}/withdraw", new WithdrawReportRequest("Not mine to withdraw."));

        withdraw.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
