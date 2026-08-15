using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;

namespace CommunityIncidentReporting.Api.Tests.Integration;

public class AdminSourceChannelTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _categoryId;
    private const string AdminPassword = "Correct-Horse-Battery-Staple-1";

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        await using var db = _factory.CreateDbContext();

        db.AdminUsers.Add(new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = "Reviewer Admin",
            Email = "reviewer@cirs.gov.sl",
            PasswordHash = new BCryptPasswordHasher().Hash(AdminPassword),
            Role = AdminRole.Reviewer,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

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

        // A WhatsApp-originated reporter/report, seeded directly (mirroring what
        // WhatsAppWebhookService produces) so both channels exist side by side.
        var whatsAppReporter = new Reporter
        {
            Id = Guid.NewGuid(),
            WhatsAppNumberHash = "seed-hash",
            MaskedContactReference = "+232 76 *** 111",
            VerificationStatus = VerificationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Reporters.Add(whatsAppReporter);
        db.IncidentReports.Add(new IncidentReport
        {
            Id = Guid.NewGuid(),
            ReporterId = whatsAppReporter.Id,
            CategoryId = category.Id,
            SourceChannel = SourceChannel.WhatsApp,
            Description = "A WhatsApp-submitted report.",
            IncidentOccurredAt = DateTimeOffset.UtcNow,
            LocationDescription = "Somewhere via WhatsApp",
            VerificationStatus = VerificationStatus.Verified,
            CaseStatus = CaseStatus.UnderReview,
            Priority = IncidentPriority.Medium,
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

    private async Task<string> AdminLoginAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/admin/auth/login", new LoginRequest("reviewer@cirs.gov.sl", AdminPassword));
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokenResponse>(JsonOptions);
        return tokens!.AccessToken;
    }

    private async Task<(Guid ReportId, string ReporterToken)> CreateVerifiedMobileReportWithAttachmentAsync()
    {
        const string email = "mobilereporter@example.com";
        await _client.PostAsJsonAsync("/api/mobile/auth/register", new RegisterReporterRequest(
            "Mobile Reporter", email, "+23276111999", "Correct-Horse-1", "Correct-Horse-1", true));
        var otpMessage = _factory.EmailService.LatestFor(email);
        var code = RecordingEmailService.ExtractOtpCode(otpMessage);
        var verify = await _client.PostAsJsonAsync("/api/mobile/auth/verify-email-otp", new VerifyEmailOtpRequest(email, code));
        var reporterTokens = await verify.Content.ReadFromJsonAsync<ReporterAuthTokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reporterTokens!.AccessToken);
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "A mobile-submitted report.", DateTimeOffset.UtcNow, "Somewhere via mobile", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        using var form = new MultipartFormDataContent();
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "files", "evidence.png");
        await _client.PostAsync($"/api/mobile/reports/{report!.Id}/attachments", form);

        _client.DefaultRequestHeaders.Authorization = null;
        return (report.Id, reporterTokens.AccessToken);
    }

    [Fact]
    public async Task AdminReportsList_FilteredBySourceChannel_ReturnsOnlyMatchingReports()
    {
        var (mobileReportId, _) = await CreateVerifiedMobileReportWithAttachmentAsync();

        await using (var db = _factory.CreateDbContext())
        {
            var mobileReport = await db.IncidentReports.FindAsync(mobileReportId);
            mobileReport!.VerificationStatus = VerificationStatus.Verified;
            mobileReport.CaseStatus = CaseStatus.UnderReview;
            await db.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AdminLoginAsync());

        var whatsAppOnly = await _client.GetAsync("/api/admin/reports?sourceChannel=WhatsApp");
        var whatsAppPage = await whatsAppOnly.Content.ReadFromJsonAsync<PagedResult<IncidentReportListItemDto>>(JsonOptions);
        whatsAppPage!.Items.Should().OnlyContain(r => r.SourceChannel == SourceChannel.WhatsApp);

        var mobileOnly = await _client.GetAsync("/api/admin/reports?sourceChannel=MobileApp");
        var mobilePage = await mobileOnly.Content.ReadFromJsonAsync<PagedResult<IncidentReportListItemDto>>(JsonOptions);
        mobilePage!.Items.Should().ContainSingle(r => r.Id == mobileReportId);
        mobilePage.Items.Single().SourceChannel.Should().Be(SourceChannel.MobileApp);
    }

    [Fact]
    public async Task AdminReportDetail_ShowsMediaAttachmentsAndSignedUrlWorks()
    {
        var (mobileReportId, _) = await CreateVerifiedMobileReportWithAttachmentAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await AdminLoginAsync());

        var detailResponse = await _client.GetAsync($"/api/admin/reports/{mobileReportId}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<IncidentReportDetailDto>(JsonOptions);
        detail!.SourceChannel.Should().Be(SourceChannel.MobileApp);
        detail.MediaAttachments.Should().ContainSingle();

        var attachmentId = detail.MediaAttachments[0].Id;
        var accessUrl = await _client.GetAsync($"/api/admin/reports/{mobileReportId}/attachments/{attachmentId}/access-url");
        accessUrl.StatusCode.Should().Be(HttpStatusCode.OK);
        var signed = await accessUrl.Content.ReadFromJsonAsync<SignedUrlResponse>();
        signed!.Url.Should().NotBeNullOrWhiteSpace();
    }
}
