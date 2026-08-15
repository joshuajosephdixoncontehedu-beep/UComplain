using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Application.Features.Clarifications;
using CommunityIncidentReporting.Application.Features.Clarifications.Dtos;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Application.Features.Verification.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIncidentReporting.Api.Tests.Integration;

public class ClarificationEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string AdminPassword = "Correct-Horse-Battery-Staple-1";

    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _categoryId;
    private string _adminEmail = null!;

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
        _categoryId = category.Id;

        _adminEmail = $"reviewer-{Guid.NewGuid():N}@cirs.gov.sl";
        db.AdminUsers.Add(new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = "Reviewer Admin",
            Email = _adminEmail,
            PasswordHash = new BCryptPasswordHasher().Hash(AdminPassword),
            Role = AdminRole.Reviewer,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
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

    private void AuthorizeReporter(string accessToken) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    private async Task AuthorizeAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", new LoginRequest(_adminEmail, AdminPassword));
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
    }

    private async Task<Guid> CreateReportAsync(string description)
    {
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, description, DateTimeOffset.UtcNow.AddHours(-1), "Somewhere", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);
        return report!.Id;
    }

    private async Task RequestClarificationAsync(Guid reportId, string message = "Please share a photo of the damage.")
    {
        await AuthorizeAdminAsync();
        var decision = await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.RequestClarification, message));
        decision.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestClarification_CreatesAThreadVisibleToTheReporter()
    {
        var reporterToken = await RegisterReporterAsync("clarify1@example.com");
        AuthorizeReporter(reporterToken);
        var reportId = await CreateReportAsync("A report needing more detail.");

        await RequestClarificationAsync(reportId, "Can you share a photo?");

        AuthorizeReporter(reporterToken);
        var response = await _client.GetAsync($"/api/mobile/reports/{reportId}/clarifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var threads = await response.Content.ReadFromJsonAsync<List<ClarificationRequestDto>>(JsonOptions);
        threads.Should().ContainSingle();
        threads![0].Message.Should().Be("Can you share a photo?");
        threads[0].ResolvedAt.Should().BeNull();
        threads[0].Responses.Should().BeEmpty();
    }

    [Fact]
    public async Task Reply_ResolvesTheClarificationRequest()
    {
        var reporterToken = await RegisterReporterAsync("clarify2@example.com");
        AuthorizeReporter(reporterToken);
        var reportId = await CreateReportAsync("A report needing more detail.");
        await RequestClarificationAsync(reportId);

        AuthorizeReporter(reporterToken);
        var threadsResponse = await _client.GetAsync($"/api/mobile/reports/{reportId}/clarifications");
        var threads = await threadsResponse.Content.ReadFromJsonAsync<List<ClarificationRequestDto>>(JsonOptions);
        var clarificationId = threads![0].Id;

        var reply = await _client.PostAsJsonAsync(
            $"/api/mobile/clarifications/{clarificationId}/reply", new ReplyToClarificationRequest("Here's more detail.", null));

        reply.StatusCode.Should().Be(HttpStatusCode.OK);
        var replyDto = await reply.Content.ReadFromJsonAsync<ClarificationResponseDto>(JsonOptions);
        replyDto!.Message.Should().Be("Here's more detail.");

        var afterReply = await _client.GetAsync($"/api/mobile/reports/{reportId}/clarifications");
        var afterThreads = await afterReply.Content.ReadFromJsonAsync<List<ClarificationRequestDto>>(JsonOptions);
        afterThreads![0].ResolvedAt.Should().NotBeNull();
        afterThreads[0].Responses.Should().ContainSingle(r => r.Id == replyDto.Id);
    }

    [Fact]
    public async Task Reply_ByNonOwningReporter_Returns404()
    {
        var ownerToken = await RegisterReporterAsync("clarify-owner@example.com");
        AuthorizeReporter(ownerToken);
        var reportId = await CreateReportAsync("Owner's report.");
        await RequestClarificationAsync(reportId);

        AuthorizeReporter(ownerToken);
        var threadsResponse = await _client.GetAsync($"/api/mobile/reports/{reportId}/clarifications");
        var threads = await threadsResponse.Content.ReadFromJsonAsync<List<ClarificationRequestDto>>(JsonOptions);
        var clarificationId = threads![0].Id;

        var intruderToken = await RegisterReporterAsync("clarify-intruder@example.com");
        AuthorizeReporter(intruderToken);
        var reply = await _client.PostAsJsonAsync(
            $"/api/mobile/clarifications/{clarificationId}/reply", new ReplyToClarificationRequest("Not mine.", null));

        reply.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reply_WithAttachmentNotOnTheReport_Returns404()
    {
        var reporterToken = await RegisterReporterAsync("clarify3@example.com");
        AuthorizeReporter(reporterToken);
        var reportId = await CreateReportAsync("A report needing more detail.");
        await RequestClarificationAsync(reportId);

        AuthorizeReporter(reporterToken);
        var threadsResponse = await _client.GetAsync($"/api/mobile/reports/{reportId}/clarifications");
        var threads = await threadsResponse.Content.ReadFromJsonAsync<List<ClarificationRequestDto>>(JsonOptions);
        var clarificationId = threads![0].Id;

        var reply = await _client.PostAsJsonAsync(
            $"/api/mobile/clarifications/{clarificationId}/reply",
            new ReplyToClarificationRequest("Here's a photo.", Guid.NewGuid()));

        reply.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reply_AfterTheReportHasAlreadyBeenApproved_Returns409()
    {
        var reporterToken = await RegisterReporterAsync("clarify4@example.com");
        AuthorizeReporter(reporterToken);
        var reportId = await CreateReportAsync("A report needing more detail.");
        await RequestClarificationAsync(reportId);

        AuthorizeReporter(reporterToken);
        var threadsResponse = await _client.GetAsync($"/api/mobile/reports/{reportId}/clarifications");
        var threads = await threadsResponse.Content.ReadFromJsonAsync<List<ClarificationRequestDto>>(JsonOptions);
        var clarificationId = threads![0].Id;

        // Admin approves the report through another channel before the reporter replies.
        await AuthorizeAdminAsync();
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));

        AuthorizeReporter(reporterToken);
        var reply = await _client.PostAsJsonAsync(
            $"/api/mobile/clarifications/{clarificationId}/reply", new ReplyToClarificationRequest("Too late.", null));

        reply.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AutoCloseSweep_ClosesOnlyUnresolvedOverdueRequests()
    {
        var reporterToken = await RegisterReporterAsync("autoclose@example.com");
        AuthorizeReporter(reporterToken);
        var overdueUnresolvedId = await CreateReportAsync("Overdue and never answered.");
        var overdueResolvedId = await CreateReportAsync("Overdue but already answered.");
        var notYetDueId = await CreateReportAsync("Not due yet.");

        await RequestClarificationAsync(overdueUnresolvedId);
        await RequestClarificationAsync(overdueResolvedId);
        await RequestClarificationAsync(notYetDueId);

        await using (var db = _factory.CreateDbContext())
        {
            var past = DateTimeOffset.UtcNow.AddHours(-1);
            var requests = await db.ClarificationRequests.ToListAsync();

            var overdueUnresolved = requests.Single(r => r.IncidentReportId == overdueUnresolvedId);
            overdueUnresolved.DueAt = past;

            var overdueResolved = requests.Single(r => r.IncidentReportId == overdueResolvedId);
            overdueResolved.DueAt = past;
            overdueResolved.ResolvedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var autoCloseService = scope.ServiceProvider.GetRequiredService<IClarificationAutoCloseService>();
        var closedCount = await autoCloseService.CloseOverdueAsync(CancellationToken.None);

        closedCount.Should().Be(1);

        await using var verifyDb = _factory.CreateDbContext();
        var overdueReport = await verifyDb.IncidentReports.SingleAsync(r => r.Id == overdueUnresolvedId);
        overdueReport.VerificationStatus.Should().Be(VerificationStatus.Rejected);
        overdueReport.CaseStatus.Should().Be(CaseStatus.Closed);
        overdueReport.ClosedAt.Should().NotBeNull();

        var resolvedReport = await verifyDb.IncidentReports.SingleAsync(r => r.Id == overdueResolvedId);
        resolvedReport.CaseStatus.Should().NotBe(CaseStatus.Closed, "it was already answered before its deadline");

        var notYetDueReport = await verifyDb.IncidentReports.SingleAsync(r => r.Id == notYetDueId);
        notYetDueReport.CaseStatus.Should().NotBe(CaseStatus.Closed, "its deadline hasn't passed yet");

        // Idempotent: sweeping again finds nothing new to close.
        var secondSweep = await autoCloseService.CloseOverdueAsync(CancellationToken.None);
        secondSweep.Should().Be(0);
    }
}
