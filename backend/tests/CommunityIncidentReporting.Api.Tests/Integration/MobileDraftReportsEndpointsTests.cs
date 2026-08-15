using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using FluentAssertions;

namespace CommunityIncidentReporting.Api.Tests.Integration;

public class MobileDraftReportsEndpointsTests : IAsyncLifetime
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
            Slug = "road-hazard",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.IncidentCategories.Add(category);

        db.IncidentCategories.Add(new IncidentCategory
        {
            Id = Guid.NewGuid(),
            Name = "Disabled Category",
            Description = "Should never appear in the mobile catalogue.",
            DefaultPriority = IncidentPriority.Low,
            SlaHours = 24,
            IsActive = false,
            DisplayOrder = 2,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

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
    public async Task GetCategories_OnlyReturnsActiveCategories()
    {
        Authorize(await RegisterReporterAsync("catalogue@example.com"));

        var response = await _client.GetAsync("/api/mobile/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<MobileCategoryDto>>(JsonOptions);
        categories.Should().ContainSingle();
        categories![0].Slug.Should().Be("road-hazard");
    }

    [Fact]
    public async Task CreateDraft_ThenUpdate_PersistsFieldsAcrossWizardSteps()
    {
        Authorize(await RegisterReporterAsync("wizard@example.com"));

        var create = await _client.PostAsync("/api/mobile/reports/drafts", null);
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var draft = await create.Content.ReadFromJsonAsync<DraftDto>(JsonOptions);
        draft!.CategoryId.Should().BeNull();

        var step1 = await _client.PatchAsJsonAsync($"/api/mobile/reports/drafts/{draft.Id}",
            new UpdateDraftRequest(_categoryId, null, null, IncidentPriority.High, null, null, null, null));
        step1.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterStep1 = await step1.Content.ReadFromJsonAsync<DraftDto>(JsonOptions);
        afterStep1!.CategoryId.Should().Be(_categoryId);
        afterStep1.CategoryName.Should().Be("Road Hazard");

        var step2 = await _client.PatchAsJsonAsync($"/api/mobile/reports/drafts/{draft.Id}",
            new UpdateDraftRequest(_categoryId, "A deep pothole on Main Street.", DateTimeOffset.UtcNow.AddHours(-2),
                IncidentPriority.High, "Near the market junction", 8.4657, -13.2317, "Blue kiosk on the corner"));
        var afterStep2 = await step2.Content.ReadFromJsonAsync<DraftDto>(JsonOptions);
        afterStep2!.Description.Should().Be("A deep pothole on Main Street.");
        afterStep2.Landmark.Should().Be("Blue kiosk on the corner");
    }

    [Fact]
    public async Task UpdateDraft_ForAnotherReportersDraft_Returns404()
    {
        Authorize(await RegisterReporterAsync("owner@example.com"));
        var create = await _client.PostAsync("/api/mobile/reports/drafts", null);
        var draft = await create.Content.ReadFromJsonAsync<DraftDto>(JsonOptions);

        Authorize(await RegisterReporterAsync("intruder@example.com"));
        var update = await _client.PatchAsJsonAsync($"/api/mobile/reports/drafts/{draft!.Id}",
            new UpdateDraftRequest(null, "hijacked", null, null, null, null, null, null));

        update.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitDraft_MissingRequiredFields_Returns409()
    {
        Authorize(await RegisterReporterAsync("incomplete@example.com"));
        var create = await _client.PostAsync("/api/mobile/reports/drafts", null);
        var draft = await create.Content.ReadFromJsonAsync<DraftDto>(JsonOptions);

        var submit = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/drafts/{draft!.Id}/submit", new SubmitDraftRequest(true));

        submit.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SubmitDraft_WithoutAcceptingTruthDeclaration_Returns422()
    {
        Authorize(await RegisterReporterAsync("notruth@example.com"));
        var create = await _client.PostAsync("/api/mobile/reports/drafts", null);
        var draft = await create.Content.ReadFromJsonAsync<DraftDto>(JsonOptions);

        var submit = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/drafts/{draft!.Id}/submit", new SubmitDraftRequest(false));

        submit.StatusCode.Should().Be((HttpStatusCode)422);
    }

    private async Task<Guid> CreateCompleteDraftAsync()
    {
        var create = await _client.PostAsync("/api/mobile/reports/drafts", null);
        var draft = await create.Content.ReadFromJsonAsync<DraftDto>(JsonOptions);

        await _client.PatchAsJsonAsync($"/api/mobile/reports/drafts/{draft!.Id}",
            new UpdateDraftRequest(_categoryId, "A deep pothole blocking one lane.", DateTimeOffset.UtcNow.AddHours(-1),
                IncidentPriority.Critical, "Main Street near the market", 8.4657, -13.2317, "Blue kiosk"));

        return draft.Id;
    }

    [Fact]
    public async Task SubmitDraft_WithCompleteData_CreatesAMobileAppReportAndMovesAttachments()
    {
        Authorize(await RegisterReporterAsync("submitter@example.com"));
        var draftId = await CreateCompleteDraftAsync();

        using (var form = new MultipartFormDataContent())
        {
            var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var fileContent = new ByteArrayContent(pngBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "files", "evidence.png");
            var upload = await _client.PostAsync($"/api/mobile/reports/drafts/{draftId}/attachments", form);
            upload.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var submit = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/drafts/{draftId}/submit", new SubmitDraftRequest(true));

        submit.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await submit.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);
        report!.SourceChannel.Should().Be(SourceChannel.MobileApp);
        // Server-controlled from the category's DefaultPriority, not the draft's InitialPrioritySignal (Critical).
        report.Priority.Should().Be(IncidentPriority.Medium);
        report.Landmark.Should().Be("Blue kiosk");
        report.Attachments.Should().ContainSingle();

        // The re-parented attachment is a real IncidentMediaAttachment now, reachable
        // through the submitted report's own (not the draft's) access-url route.
        var accessUrl = await _client.GetAsync(
            $"/api/mobile/reports/{report.Id}/attachments/{report.Attachments[0].Id}/access-url");
        accessUrl.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitDraft_CalledTwice_IsIdempotentAndReturnsTheSameReport()
    {
        Authorize(await RegisterReporterAsync("retry@example.com"));
        var draftId = await CreateCompleteDraftAsync();

        var firstSubmit = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/drafts/{draftId}/submit", new SubmitDraftRequest(true));
        var firstReport = await firstSubmit.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        var secondSubmit = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/drafts/{draftId}/submit", new SubmitDraftRequest(true));

        secondSubmit.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondReport = await secondSubmit.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);
        secondReport!.Id.Should().Be(firstReport!.Id);
        secondReport.CaseReference.Should().Be(firstReport.CaseReference);

        // Exactly one report was created — the retry did not duplicate it.
        var myReports = await _client.GetAsync("/api/mobile/reports");
        var page = await myReports.Content.ReadFromJsonAsync<CommunityIncidentReporting.Application.Common.Models.PagedResult<MobileReportListItemDto>>(JsonOptions);
        page!.Total.Should().Be(1);
    }

    [Fact]
    public async Task UpdateDraft_AfterSubmission_Returns409()
    {
        Authorize(await RegisterReporterAsync("locked@example.com"));
        var draftId = await CreateCompleteDraftAsync();
        await _client.PostAsJsonAsync($"/api/mobile/reports/drafts/{draftId}/submit", new SubmitDraftRequest(true));

        var update = await _client.PatchAsJsonAsync($"/api/mobile/reports/drafts/{draftId}",
            new UpdateDraftRequest(null, "too late", null, null, null, null, null, null));

        update.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
