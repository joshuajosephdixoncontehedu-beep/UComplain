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
using FluentAssertions;

namespace CommunityIncidentReporting.Api.Tests.Integration;

public class MobileReportsEndpointsTests : IAsyncLifetime
{
    // Matches the server's AddJsonOptions(JsonStringEnumConverter) in Program.cs — see ReportsAndVerificationTests.
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

    [Fact]
    public async Task Create_SetsSourceChannelAndServerControlledFields()
    {
        Authorize(await RegisterReporterAsync("reporter1@example.com"));

        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "A large pothole blocking traffic.", DateTimeOffset.UtcNow.AddHours(-1),
            "Main Street near the market", 8.4657, -13.2317, IncidentPriority.Critical));

        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);
        report!.SourceChannel.Should().Be(SourceChannel.MobileApp);
        report.CaseStatus.Should().Be(CaseStatus.VerificationPending);
        report.VerificationStatus.Should().Be(VerificationStatus.Pending);
        // Server-controlled from the category's DefaultPriority, not the client's InitialPrioritySignal (Critical).
        report.Priority.Should().Be(IncidentPriority.Medium);
        report.CaseReference.Should().StartWith("CIRS-");
    }

    [Fact]
    public async Task GetMyReports_OnlyReturnsTheAuthenticatedReportersOwnReports()
    {
        var reporterA = await RegisterReporterAsync("reporterA@example.com");
        Authorize(reporterA);
        await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "Report from reporter A.", DateTimeOffset.UtcNow, "Location A", null, null, null));

        var reporterB = await RegisterReporterAsync("reporterB@example.com");
        Authorize(reporterB);
        var listForB = await _client.GetAsync("/api/mobile/reports");
        var pageForB = await listForB.Content.ReadFromJsonAsync<PagedResult<MobileReportListItemDto>>(JsonOptions);

        pageForB!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ForAnotherReportersReport_Returns404()
    {
        Authorize(await RegisterReporterAsync("owner@example.com"));
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "Owner's report.", DateTimeOffset.UtcNow, "Somewhere", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        Authorize(await RegisterReporterAsync("intruder@example.com"));
        var response = await _client.GetAsync($"/api/mobile/reports/{report!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadAttachment_ThenFetchAccessUrl_RoundTripsThroughStorage()
    {
        Authorize(await RegisterReporterAsync("uploader@example.com"));
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "Report with a photo.", DateTimeOffset.UtcNow, "Somewhere", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        using var form = new MultipartFormDataContent();
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var fileContent = new ByteArrayContent(jpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "files", "pothole.jpg");

        var upload = await _client.PostAsync($"/api/mobile/reports/{report!.Id}/attachments", form);
        upload.StatusCode.Should().Be(HttpStatusCode.OK);
        var attachments = await upload.Content.ReadFromJsonAsync<List<MediaAttachmentDto>>(JsonOptions);
        attachments.Should().HaveCount(1);
        attachments![0].MediaType.Should().Be(MediaType.Image);

        _factory.StorageService.Objects.Should().ContainSingle();

        var accessUrl = await _client.GetAsync(
            $"/api/mobile/reports/{report.Id}/attachments/{attachments[0].Id}/access-url");
        accessUrl.StatusCode.Should().Be(HttpStatusCode.OK);
        var signed = await accessUrl.Content.ReadFromJsonAsync<SignedUrlResponse>();
        signed!.Url.Should().Contain("token=");
    }

    [Fact]
    public async Task UploadAttachment_WithMismatchedMagicBytes_Returns409()
    {
        Authorize(await RegisterReporterAsync("faker@example.com"));
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "Report with a fake photo.", DateTimeOffset.UtcNow, "Somewhere", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        using var form = new MultipartFormDataContent();
        var notActuallyAJpeg = "this is just plain text, not a jpeg"u8.ToArray();
        var fileContent = new ByteArrayContent(notActuallyAJpeg);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "files", "fake.jpg");

        var upload = await _client.PostAsync($"/api/mobile/reports/{report!.Id}/attachments", form);

        upload.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _factory.StorageService.Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAttachment_ByNonOwner_Returns404AndLeavesAttachmentIntact()
    {
        Authorize(await RegisterReporterAsync("owner2@example.com"));
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "Report with a photo.", DateTimeOffset.UtcNow, "Somewhere", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        using var form = new MultipartFormDataContent();
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "files", "sign.png");
        var upload = await _client.PostAsync($"/api/mobile/reports/{report!.Id}/attachments", form);
        var attachments = await upload.Content.ReadFromJsonAsync<List<MediaAttachmentDto>>(JsonOptions);

        Authorize(await RegisterReporterAsync("intruder2@example.com"));
        var delete = await _client.DeleteAsync($"/api/mobile/reports/{report.Id}/attachments/{attachments![0].Id}");

        delete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.StorageService.Objects.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadAttachment_ExceedingTheOversizeLimit_Returns409AndUploadsNothing()
    {
        // appsettings.Testing.json caps MaxImageSizeBytes at 1024 for fast, deterministic testing.
        Authorize(await RegisterReporterAsync("oversize@example.com"));
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "Report with an oversized photo.", DateTimeOffset.UtcNow, "Somewhere", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        using var form = new MultipartFormDataContent();
        var oversizedJpeg = new byte[2000];
        oversizedJpeg[0] = 0xFF; oversizedJpeg[1] = 0xD8; oversizedJpeg[2] = 0xFF;
        var fileContent = new ByteArrayContent(oversizedJpeg);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "files", "huge.jpg");

        var upload = await _client.PostAsync($"/api/mobile/reports/{report!.Id}/attachments", form);

        upload.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _factory.StorageService.Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAttachment_ExceedingTheMaxCountForAType_Returns409AndUploadsNothingFromThatBatch()
    {
        // appsettings.Testing.json caps MaxImages at 2.
        Authorize(await RegisterReporterAsync("toomany@example.com"));
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "Report with too many photos.", DateTimeOffset.UtcNow, "Somewhere", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        using var form = new MultipartFormDataContent();
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        for (var i = 0; i < 3; i++)
        {
            var fileContent = new ByteArrayContent(pngBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "files", $"photo{i}.png");
        }

        var upload = await _client.PostAsync($"/api/mobile/reports/{report!.Id}/attachments", form);

        upload.StatusCode.Should().Be(HttpStatusCode.Conflict);
        _factory.StorageService.Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAttachmentAccessUrl_ByNonOwner_Returns404()
    {
        Authorize(await RegisterReporterAsync("owner3@example.com"));
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "Report with a photo.", DateTimeOffset.UtcNow, "Somewhere", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        using var form = new MultipartFormDataContent();
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var fileContent = new ByteArrayContent(pngBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "files", "evidence.png");
        var upload = await _client.PostAsync($"/api/mobile/reports/{report!.Id}/attachments", form);
        var attachments = await upload.Content.ReadFromJsonAsync<List<MediaAttachmentDto>>(JsonOptions);

        Authorize(await RegisterReporterAsync("intruder3@example.com"));
        var accessUrl = await _client.GetAsync(
            $"/api/mobile/reports/{report.Id}/attachments/{attachments![0].Id}/access-url");

        accessUrl.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WritesAnAuditLogRowForTheSubmission()
    {
        var accessToken = await RegisterReporterAsync("audited@example.com");
        Authorize(accessToken);

        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "An audited report.", DateTimeOffset.UtcNow, "Somewhere", null, null, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        await using var db = _factory.CreateDbContext();
        var auditRow = db.AuditLogs.SingleOrDefault(
            l => l.Action == "ReportSubmittedViaMobileApp" && l.EntityId == report!.Id.ToString());

        auditRow.Should().NotBeNull();
        auditRow!.AdminUserId.Should().BeNull("mobile submissions are reporter-initiated, not an admin action");
        auditRow.NewValueJson.Should().Contain(_categoryId.ToString());
    }
}
