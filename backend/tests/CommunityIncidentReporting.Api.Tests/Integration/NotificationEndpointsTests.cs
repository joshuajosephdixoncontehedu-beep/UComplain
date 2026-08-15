using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Application.Features.Clarifications;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Application.Features.Notifications.Dtos;
using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using CommunityIncidentReporting.Application.Features.Verification.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIncidentReporting.Api.Tests.Integration;

public class NotificationEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string AdminPassword = "Correct-Horse-Battery-Staple-1";

    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _categoryId;
    private Guid _incidentManagerId;
    private string _managerEmail = null!;
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

        _managerEmail = $"manager-{Guid.NewGuid():N}@cirs.gov.sl";
        var manager = new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = "Incident Manager",
            Email = _managerEmail,
            PasswordHash = new BCryptPasswordHasher().Hash(AdminPassword),
            Role = AdminRole.IncidentManager,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.AdminUsers.Add(manager);
        _incidentManagerId = manager.Id;

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

    private async Task AuthorizeAdminAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", new LoginRequest(email, AdminPassword));
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

    private async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(string reporterToken)
    {
        AuthorizeReporter(reporterToken);
        var response = await _client.GetAsync("/api/mobile/notifications");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>(JsonOptions);
        return page!.Items;
    }

    [Fact]
    public async Task ApprovingAReport_NotifiesTheReporter()
    {
        var reporterToken = await RegisterReporterAsync("notify-approve@example.com");
        AuthorizeReporter(reporterToken);
        var reportId = await CreateReportAsync("A report awaiting approval.");

        await AuthorizeAdminAsync(_reviewerEmail);
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));

        var notifications = await GetNotificationsAsync(reporterToken);
        notifications.Should().ContainSingle(n => n.Type == NotificationType.ReportVerified && n.ReportId == reportId);
    }

    [Fact]
    public async Task RequestingClarification_NotifiesTheReporterWithTheAdminsMessage()
    {
        var reporterToken = await RegisterReporterAsync("notify-clarify@example.com");
        AuthorizeReporter(reporterToken);
        var reportId = await CreateReportAsync("A report needing more detail.");

        await AuthorizeAdminAsync(_reviewerEmail);
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.RequestClarification, "Please share a photo."));

        var notifications = await GetNotificationsAsync(reporterToken);
        var notification = notifications.Should().ContainSingle(n => n.Type == NotificationType.ClarificationRequested).Subject;
        notification.Body.Should().Contain("Please share a photo.");
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public async Task AssigningAReport_NotifiesTheReporter()
    {
        var reporterToken = await RegisterReporterAsync("notify-assign@example.com");
        AuthorizeReporter(reporterToken);
        var reportId = await CreateReportAsync("A report to be assigned.");

        await AuthorizeAdminAsync(_reviewerEmail);
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));

        await AuthorizeAdminAsync(_managerEmail);
        await _client.PostAsJsonAsync($"/api/admin/reports/{reportId}/assign", new AssignReportRequest(_incidentManagerId));

        var notifications = await GetNotificationsAsync(reporterToken);
        notifications.Should().Contain(n => n.Type == NotificationType.AssignmentMade);
    }

    [Fact]
    public async Task ChangingStatusToInProgressThenResolved_NotifiesTheReporterForEach()
    {
        var reporterToken = await RegisterReporterAsync("notify-status@example.com");
        AuthorizeReporter(reporterToken);
        var reportId = await CreateReportAsync("A report moving through the workflow.");

        await AuthorizeAdminAsync(_reviewerEmail);
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));

        await AuthorizeAdminAsync(_managerEmail);
        await _client.PostAsJsonAsync($"/api/admin/reports/{reportId}/assign", new AssignReportRequest(_incidentManagerId));
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/status", new ChangeStatusRequest(CaseStatus.InProgress, "Starting work."));
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/status", new ChangeStatusRequest(CaseStatus.Resolved, "Fixed."));

        var notifications = await GetNotificationsAsync(reporterToken);
        notifications.Should().Contain(n => n.Type == NotificationType.WorkStarted);
        notifications.Should().Contain(n => n.Type == NotificationType.ReportResolved);
    }

    [Fact]
    public async Task MarkRead_IsIdempotentAndOwnershipScoped()
    {
        var reporterToken = await RegisterReporterAsync("notify-read@example.com");
        AuthorizeReporter(reporterToken);
        var reportId = await CreateReportAsync("A report to trigger a notification.");

        await AuthorizeAdminAsync(_reviewerEmail);
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));

        var notifications = await GetNotificationsAsync(reporterToken);
        var notificationId = notifications.Single().Id;

        AuthorizeReporter(reporterToken);
        var firstRead = await _client.PostAsync($"/api/mobile/notifications/{notificationId}/read", null);
        firstRead.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstDto = await firstRead.Content.ReadFromJsonAsync<NotificationDto>(JsonOptions);
        firstDto!.ReadAt.Should().NotBeNull();

        var secondRead = await _client.PostAsync($"/api/mobile/notifications/{notificationId}/read", null);
        var secondDto = await secondRead.Content.ReadFromJsonAsync<NotificationDto>(JsonOptions);
        secondDto!.ReadAt.Should().Be(firstDto.ReadAt, "reading an already-read notification again must not change its timestamp");

        var intruderToken = await RegisterReporterAsync("notify-intruder@example.com");
        AuthorizeReporter(intruderToken);
        var intruderRead = await _client.PostAsync($"/api/mobile/notifications/{notificationId}/read", null);
        intruderRead.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAllRead_MarksEveryUnreadNotificationAndReturnsTheCount()
    {
        var reporterToken = await RegisterReporterAsync("notify-readall@example.com");
        AuthorizeReporter(reporterToken);
        var reportA = await CreateReportAsync("Report A.");
        var reportB = await CreateReportAsync("Report B.");

        await AuthorizeAdminAsync(_reviewerEmail);
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportA}/verification-decision", new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportB}/verification-decision", new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));

        AuthorizeReporter(reporterToken);
        var response = await _client.PostAsync("/api/mobile/notifications/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MarkAllReadResponse>(JsonOptions);
        result!.UpdatedCount.Should().Be(2);

        var notifications = await GetNotificationsAsync(reporterToken);
        notifications.Should().OnlyContain(n => n.ReadAt != null);
    }

    [Fact]
    public async Task AutoClosingAReport_NotifiesTheReporter()
    {
        var reporterToken = await RegisterReporterAsync("notify-autoclose@example.com");
        AuthorizeReporter(reporterToken);
        var reportId = await CreateReportAsync("A report that will go unanswered.");

        await AuthorizeAdminAsync(_reviewerEmail);
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.RequestClarification, "Please respond."));

        await using (var db = _factory.CreateDbContext())
        {
            var clarification = await db.ClarificationRequests.SingleAsync(c => c.IncidentReportId == reportId);
            clarification.DueAt = DateTimeOffset.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();
        }

        using var scope = _factory.Services.CreateScope();
        var autoCloseService = scope.ServiceProvider.GetRequiredService<IClarificationAutoCloseService>();
        await autoCloseService.CloseOverdueAsync(CancellationToken.None);

        var notifications = await GetNotificationsAsync(reporterToken);
        notifications.Should().Contain(n => n.Type == NotificationType.ReportAutoClosed && n.ReportId == reportId);
    }

    [Fact]
    public async Task RegisterDevice_ThenReRegisterSameToken_ReassignsItToTheNewReporter()
    {
        var firstReporterToken = await RegisterReporterAsync("device-first@example.com");
        AuthorizeReporter(firstReporterToken);
        const string sharedToken = "expo-push-token-shared-device";

        var firstRegister = await _client.PostAsJsonAsync(
            "/api/mobile/devices", new RegisterDeviceRequest(DevicePlatform.Android, sharedToken));
        firstRegister.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstDevice = await firstRegister.Content.ReadFromJsonAsync<DeviceTokenDto>(JsonOptions);

        var secondReporterToken = await RegisterReporterAsync("device-second@example.com");
        AuthorizeReporter(secondReporterToken);
        var secondRegister = await _client.PostAsJsonAsync(
            "/api/mobile/devices", new RegisterDeviceRequest(DevicePlatform.Android, sharedToken));
        var secondDevice = await secondRegister.Content.ReadFromJsonAsync<DeviceTokenDto>(JsonOptions);

        secondDevice!.Id.Should().Be(firstDevice!.Id, "the same token upserts the same row rather than duplicating it");

        await using var db = _factory.CreateDbContext();
        var stored = await db.DeviceTokens.SingleAsync(d => d.Token == sharedToken);
        var secondReporterId = (await db.Reporters.SingleAsync(r => r.Email == "device-second@example.com")).Id;
        stored.ReporterId.Should().Be(secondReporterId);
    }

    [Fact]
    public async Task RevokeDevice_IsIdempotentAndOwnershipScoped()
    {
        var reporterToken = await RegisterReporterAsync("device-revoke@example.com");
        AuthorizeReporter(reporterToken);
        var register = await _client.PostAsJsonAsync(
            "/api/mobile/devices", new RegisterDeviceRequest(DevicePlatform.Ios, "ios-token-to-revoke"));
        var device = await register.Content.ReadFromJsonAsync<DeviceTokenDto>(JsonOptions);

        var firstDelete = await _client.DeleteAsync($"/api/mobile/devices/{device!.Id}");
        firstDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondDelete = await _client.DeleteAsync($"/api/mobile/devices/{device.Id}");
        secondDelete.StatusCode.Should().Be(HttpStatusCode.NoContent, "revoking an already-revoked device is a no-op, not an error");

        var otherReporterToken = await RegisterReporterAsync("device-other-owner@example.com");
        AuthorizeReporter(otherReporterToken);
        var registerOther = await _client.PostAsJsonAsync(
            "/api/mobile/devices", new RegisterDeviceRequest(DevicePlatform.Ios, "ios-token-owned-by-someone-else"));
        var otherDevice = await registerOther.Content.ReadFromJsonAsync<DeviceTokenDto>(JsonOptions);

        AuthorizeReporter(reporterToken);
        var crossOwnerDelete = await _client.DeleteAsync($"/api/mobile/devices/{otherDevice!.Id}");
        crossOwnerDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
