using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Application.Features.PublicMap.Dtos;
using CommunityIncidentReporting.Application.Features.ReporterAccount;
using CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;
using CommunityIncidentReporting.Application.Features.Verification.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIncidentReporting.Api.Tests.Integration;

public class ReporterAccountEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string AdminPassword = "Correct-Horse-Battery-Staple-1";
    private const double FreetownLat = 8.4657;
    private const double FreetownLng = -13.2317;

    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _categoryId;
    private string _reviewerEmail = null!;

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

        _reviewerEmail = $"reviewer-{Guid.NewGuid():N}@cirs.gov.sl";
        db.AdminUsers.Add(new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = "Reviewer Admin",
            Email = _reviewerEmail,
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

    private void ClearAuthorization() => _client.DefaultRequestHeaders.Authorization = null;

    private async Task AuthorizeAdminAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", new LoginRequest(_reviewerEmail, AdminPassword));
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
    }

    private async Task<Guid> CreateAndApproveReportAsync(double lat, double lng)
    {
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "A report for privacy testing.", DateTimeOffset.UtcNow.AddHours(-1), "Somewhere", lat, lng, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);
        var reportId = report!.Id;
        var reporterAuthHeader = _client.DefaultRequestHeaders.Authorization;

        await AuthorizeAdminAsync();
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));

        _client.DefaultRequestHeaders.Authorization = reporterAuthHeader;
        return reportId;
    }

    [Fact]
    public async Task GetPrivacy_ReturnsSensibleDefaultsOnFirstCall()
    {
        AuthorizeReporter(await RegisterReporterAsync("privacy-default@example.com"));

        var response = await _client.GetAsync("/api/mobile/me/privacy");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var privacy = await response.Content.ReadFromJsonAsync<ReporterPrivacySettingDto>(JsonOptions);
        privacy!.UsePreciseLocation.Should().BeTrue();
        privacy.ShowOnPublicMap.Should().BeTrue();
        privacy.AllowResponderContact.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePrivacy_TurningOffShowOnPublicMap_RemovesExistingReportsFromThePublicMapImmediately()
    {
        AuthorizeReporter(await RegisterReporterAsync("privacy-toggle@example.com"));
        var reportId = await CreateAndApproveReportAsync(FreetownLat, FreetownLng);

        var beforeMap = await _client.GetAsync($"/api/mobile/public/incidents?lat={FreetownLat}&lng={FreetownLng}");
        var beforeIncidents = await beforeMap.Content.ReadFromJsonAsync<List<PublicIncidentDto>>(JsonOptions);
        beforeIncidents.Should().Contain(i => i.Id == reportId);

        var update = await _client.PutAsJsonAsync(
            "/api/mobile/me/privacy", new UpdateReporterPrivacySettingRequest(true, false, true));
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<ReporterPrivacySettingDto>(JsonOptions);
        updated!.ShowOnPublicMap.Should().BeFalse();

        var afterMap = await _client.GetAsync($"/api/mobile/public/incidents?lat={FreetownLat}&lng={FreetownLng}");
        var afterIncidents = await afterMap.Content.ReadFromJsonAsync<List<PublicIncidentDto>>(JsonOptions);
        afterIncidents.Should().NotContain(i => i.Id == reportId);
    }

    [Fact]
    public async Task GetStats_ReflectsTheCallersOwnReportCounts()
    {
        AuthorizeReporter(await RegisterReporterAsync("stats@example.com"));
        await CreateAndApproveReportAsync(FreetownLat, FreetownLng);

        var response = await _client.GetAsync("/api/mobile/me/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<ReporterStatsDto>(JsonOptions);
        stats!.TotalReports.Should().Be(1);
        stats.ActiveReports.Should().Be(1);
    }

    [Fact]
    public async Task UpdateProfile_ChangesFullNameAndLanguagePreference()
    {
        AuthorizeReporter(await RegisterReporterAsync("profile@example.com"));

        var response = await _client.PatchAsJsonAsync(
            "/api/mobile/me", new UpdateMyProfileRequest("New Display Name", "kri"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ReporterProfileDto>(JsonOptions);
        profile!.FullName.Should().Be("New Display Name");
        profile.LanguagePreference.Should().Be("kri");
    }

    [Fact]
    public async Task RequestDataExport_ThenProcessSweep_CompletesAndBecomesDownloadable()
    {
        AuthorizeReporter(await RegisterReporterAsync("export@example.com"));
        await CreateAndApproveReportAsync(FreetownLat, FreetownLng);

        var firstRequest = await _client.PostAsync("/api/mobile/me/data-export", null);
        firstRequest.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstDto = await firstRequest.Content.ReadFromJsonAsync<DataExportRequestDto>(JsonOptions);
        firstDto!.Status.Should().Be(DataExportStatus.Pending);

        // Requesting again while still Pending returns the same request, not a duplicate.
        var secondRequest = await _client.PostAsync("/api/mobile/me/data-export", null);
        var secondDto = await secondRequest.Content.ReadFromJsonAsync<DataExportRequestDto>(JsonOptions);
        secondDto!.Id.Should().Be(firstDto.Id);

        using (var scope = _factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IDataExportProcessorService>();
            var processedCount = await processor.ProcessPendingAsync(CancellationToken.None);
            processedCount.Should().Be(1);
        }

        _factory.StorageService.Objects.Should().ContainSingle(o => o.Key.Contains(firstDto.Id.ToString()));

        var status = await _client.GetAsync("/api/mobile/me/data-export");
        status.StatusCode.Should().Be(HttpStatusCode.OK);
        var statusDto = await status.Content.ReadFromJsonAsync<DataExportRequestDto>(JsonOptions);
        statusDto!.Status.Should().Be(DataExportStatus.Completed);
        statusDto.DownloadUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetLatestDataExport_WhenNeverRequested_Returns404()
    {
        AuthorizeReporter(await RegisterReporterAsync("no-export@example.com"));

        var response = await _client.GetAsync("/api/mobile/me/data-export");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RequestAccountDeletion_ThenCancel_LeavesTheAccountUntouched()
    {
        var email = "delete-cancel@example.com";
        AuthorizeReporter(await RegisterReporterAsync(email));

        var request = await _client.DeleteAsync("/api/mobile/me");
        request.StatusCode.Should().Be(HttpStatusCode.OK);
        var requestDto = await request.Content.ReadFromJsonAsync<AccountDeletionRequestDto>(JsonOptions);
        requestDto!.Status.Should().Be(AccountDeletionStatus.Pending);
        requestDto.ScheduledForAt.Should().BeAfter(DateTimeOffset.UtcNow);

        // Requesting again while still Pending returns the same request, not a duplicate.
        var secondRequest = await _client.DeleteAsync("/api/mobile/me");
        var secondDto = await secondRequest.Content.ReadFromJsonAsync<AccountDeletionRequestDto>(JsonOptions);
        secondDto!.Id.Should().Be(requestDto.Id);

        var cancel = await _client.PostAsync("/api/mobile/me/deletion/cancel", null);
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelDto = await cancel.Content.ReadFromJsonAsync<AccountDeletionRequestDto>(JsonOptions);
        cancelDto!.Status.Should().Be(AccountDeletionStatus.Cancelled);

        await using var db = _factory.CreateDbContext();
        var reporter = await db.Reporters.SingleAsync(r => r.Email == email);
        reporter.AnonymizedAt.Should().BeNull();
        reporter.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CancelAccountDeletion_WithNothingPending_Returns409()
    {
        AuthorizeReporter(await RegisterReporterAsync("no-pending-deletion@example.com"));

        var response = await _client.PostAsync("/api/mobile/me/deletion/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AccountDeletionSweep_AnonymizesTheAccountAndRevokesSessions()
    {
        const string email = "delete-executed@example.com";
        var accessToken = await RegisterReporterAsync(email);
        AuthorizeReporter(accessToken);

        var request = await _client.DeleteAsync("/api/mobile/me");
        var requestDto = await request.Content.ReadFromJsonAsync<AccountDeletionRequestDto>(JsonOptions);

        await using (var db = _factory.CreateDbContext())
        {
            var deletionRequest = await db.AccountDeletionRequests.SingleAsync(d => d.Id == requestDto!.Id);
            deletionRequest.ScheduledForAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IAccountDeletionProcessorService>();
            var processedCount = await processor.ProcessDueAsync(CancellationToken.None);
            processedCount.Should().Be(1);
        }

        await using var verifyDb = _factory.CreateDbContext();
        var reporter = await verifyDb.Reporters.SingleAsync(r => r.NormalizedEmail == null && r.AnonymizedAt != null);
        reporter.Email.Should().BeNull();
        reporter.FullName.Should().BeNull();
        reporter.IsActive.Should().BeFalse();
        reporter.AnonymizedAt.Should().NotBeNull();

        var refreshTokens = await verifyDb.ReporterRefreshTokens.Where(t => t.ReporterId == reporter.Id).ToListAsync();
        refreshTokens.Should().NotBeEmpty();
        refreshTokens.Should().OnlyContain(t => t.RevokedAt != null);
    }

    [Fact]
    public async Task RetentionPurge_AnonymizesOnlyReportersInactiveBeyondTheConfiguredWindow()
    {
        var staleEmail = "retention-stale@example.com";
        var freshEmail = "retention-fresh@example.com";
        await RegisterReporterAsync(staleEmail);
        await RegisterReporterAsync(freshEmail);

        await using (var db = _factory.CreateDbContext())
        {
            db.SystemSettings.Add(new SystemSettings
            {
                Id = Guid.NewGuid(),
                OrganizationName = "UComplain",
                OrganizationContactEmail = "operations@ucomplain.com",
                ReporterDataRetentionMonths = 6,
                AuditLogRetentionMonths = 60,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            var stale = await db.Reporters.SingleAsync(r => r.Email == staleEmail);
            stale.LastLoginAt = DateTimeOffset.UtcNow.AddMonths(-7);

            var fresh = await db.Reporters.SingleAsync(r => r.Email == freshEmail);
            fresh.LastLoginAt = DateTimeOffset.UtcNow.AddDays(-1);

            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var purgeService = scope.ServiceProvider.GetRequiredService<IReporterRetentionPurgeService>();
        var purgedCount = await purgeService.PurgeInactiveAsync(CancellationToken.None);

        purgedCount.Should().Be(1);

        await using var verifyDb = _factory.CreateDbContext();
        var staleReporter = await verifyDb.Reporters.SingleAsync(r => r.AnonymizedAt != null);
        staleReporter.NormalizedEmail.Should().BeNull("the stale reporter's original email was scrubbed");

        var freshReporter = await verifyDb.Reporters.SingleAsync(r => r.Email == freshEmail);
        freshReporter.AnonymizedAt.Should().BeNull();
    }
}
