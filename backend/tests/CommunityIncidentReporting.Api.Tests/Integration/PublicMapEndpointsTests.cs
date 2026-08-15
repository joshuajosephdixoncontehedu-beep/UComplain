using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using CommunityIncidentReporting.Application.Features.PublicMap.Dtos;
using CommunityIncidentReporting.Application.Features.Verification.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Api.Tests.Integration;

public class PublicMapEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private const string AdminPassword = "Correct-Horse-Battery-Staple-1";

    // Freetown, Sierra Leone — arbitrary real-world coordinates for realistic test data.
    private const double FreetownLat = 8.4657;
    private const double FreetownLng = -13.2317;

    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _categoryId;
    private Guid _otherCategoryId;
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
            IconKey = "road",
            ColourToken = "amber",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var otherCategory = new IncidentCategory
        {
            Id = Guid.NewGuid(),
            Name = "Flooding",
            Description = "Flood-related reports.",
            DefaultPriority = IncidentPriority.High,
            SlaHours = 12,
            IsActive = true,
            DisplayOrder = 2,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.IncidentCategories.AddRange(category, otherCategory);
        _categoryId = category.Id;
        _otherCategoryId = otherCategory.Id;

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

    private async Task<Guid> CreateAndApproveReportAsync(
        string email, double lat, double lng, Guid? categoryId = null)
    {
        var reporterToken = await RegisterReporterAsync(email);
        AuthorizeReporter(reporterToken);
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            categoryId ?? _categoryId, "A report for the public map.", DateTimeOffset.UtcNow.AddHours(-1),
            "Somewhere", lat, lng, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);
        var reportId = report!.Id;

        await AuthorizeAdminAsync();
        await _client.PostAsJsonAsync(
            $"/api/admin/reports/{reportId}/verification-decision",
            new VerificationDecisionRequest(VerificationDecisionAction.Approve, null));

        ClearAuthorization();
        return reportId;
    }

    private async Task<List<PublicIncidentDto>> GetNearbyAsync(
        double lat = FreetownLat, double lng = FreetownLng, double? radiusM = null, string? categories = null)
    {
        ClearAuthorization();
        var url = $"/api/mobile/public/incidents?lat={lat}&lng={lng}";
        if (radiusM is { } r)
        {
            url += $"&radiusM={r}";
        }
        if (categories is not null)
        {
            url += $"&categories={categories}";
        }

        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var incidents = await response.Content.ReadFromJsonAsync<List<PublicIncidentDto>>(JsonOptions);
        return incidents!;
    }

    [Fact]
    public async Task GetNearby_IsReachableWithoutAnyAuthorization()
    {
        ClearAuthorization();

        var response = await _client.GetAsync($"/api/mobile/public/incidents?lat={FreetownLat}&lng={FreetownLng}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetNearby_ReturnsAnApprovedReportNearTheQueryPoint()
    {
        var reportId = await CreateAndApproveReportAsync("map-approved@example.com", FreetownLat, FreetownLng);

        var incidents = await GetNearbyAsync();

        incidents.Should().ContainSingle(i => i.Id == reportId);
        var incident = incidents.Single(i => i.Id == reportId);
        incident.CategoryName.Should().Be("Road Hazard");
        incident.CategoryIconKey.Should().Be("road");
        incident.AgeBucket.Should().Be(PublicIncidentAgeBucket.Today);
        incident.DistanceMeters.Should().BeLessThan(50);
    }

    [Fact]
    public async Task GetNearby_NeverReturnsAnUnverifiedReport()
    {
        var reporterToken = await RegisterReporterAsync("map-unverified@example.com");
        AuthorizeReporter(reporterToken);
        var create = await _client.PostAsJsonAsync("/api/mobile/reports", new CreateMobileReportRequest(
            _categoryId, "Still awaiting verification.", DateTimeOffset.UtcNow.AddHours(-1), "Somewhere",
            FreetownLat, FreetownLng, null));
        var report = await create.Content.ReadFromJsonAsync<MobileReportDetailDto>(JsonOptions);

        var incidents = await GetNearbyAsync();

        incidents.Should().NotContain(i => i.Id == report!.Id);
    }

    [Fact]
    public async Task GetNearby_NeverReturnsAWithdrawnReport_EvenThoughVerificationStatusStaysVerified()
    {
        const string ownerEmail = "map-withdrawn@example.com";
        var reportId = await CreateAndApproveReportAsync(ownerEmail, FreetownLat, FreetownLng);

        await using (var db = _factory.CreateDbContext())
        {
            var report = await db.IncidentReports.SingleAsync(r => r.Id == reportId);
            report.VerificationStatus.Should().Be(
                VerificationStatus.Verified, "sanity check: withdrawal must not rely on VerificationStatus changing");
        }

        var ownerToken = await LoginExistingReporterAsync(ownerEmail);
        AuthorizeReporter(ownerToken);
        var withdraw = await _client.PostAsJsonAsync(
            $"/api/mobile/reports/{reportId}/withdraw", new WithdrawReportRequest("No longer an issue."));
        withdraw.StatusCode.Should().Be(HttpStatusCode.OK);

        var incidents = await GetNearbyAsync();

        incidents.Should().NotContain(i => i.Id == reportId);
    }

    private async Task<string> LoginExistingReporterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/mobile/auth/login", new ReporterLoginRequest(email, "Correct-Horse-1", true));
        var tokens = await response.Content.ReadFromJsonAsync<ReporterAuthTokenResponse>(JsonOptions);
        return tokens!.AccessToken;
    }

    [Fact]
    public async Task GetNearby_NeverReturnsAReportWhoseReporterOptedOut_EvenIfIsPubliclyVisibleWasLeftTrue()
    {
        var reportId = await CreateAndApproveReportAsync("map-optout@example.com", FreetownLat, FreetownLng);

        await using (var db = _factory.CreateDbContext())
        {
            var reporter = await db.Reporters.SingleAsync(r => r.Email == "map-optout@example.com");
            db.ReporterPrivacySettings.Add(new ReporterPrivacySetting
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
                ShowOnPublicMap = false,
                UsePreciseLocation = true,
                AllowResponderContact = true,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            // Deliberately force the cached flag true to prove the query never solely
            // trusts it — the live join against ReporterPrivacySetting is the real gate.
            var report = await db.IncidentReports.SingleAsync(r => r.Id == reportId);
            report.IsPubliclyVisible = true;
            await db.SaveChangesAsync();
        }

        var incidents = await GetNearbyAsync();

        incidents.Should().NotContain(i => i.Id == reportId);
    }

    [Fact]
    public async Task GetNearby_CoarsensLocationWhenUsePreciseLocationIsOff()
    {
        var reportId = await CreateAndApproveReportAsync("map-coarse@example.com", 8.46567891, -13.23178912);

        await using (var db = _factory.CreateDbContext())
        {
            var reporter = await db.Reporters.SingleAsync(r => r.Email == "map-coarse@example.com");
            db.ReporterPrivacySettings.Add(new ReporterPrivacySetting
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
                ShowOnPublicMap = true,
                UsePreciseLocation = false,
                AllowResponderContact = true,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var incidents = await GetNearbyAsync();

        var incident = incidents.Single(i => i.Id == reportId);
        incident.Latitude.Should().Be(Math.Round(8.46567891, 2));
        incident.Longitude.Should().Be(Math.Round(-13.23178912, 2));
    }

    [Fact]
    public async Task GetNearby_ExcludesReportsOutsideTheRequestedRadius()
    {
        // Roughly 20km north of Freetown — well outside a 1km radius.
        var farReportId = await CreateAndApproveReportAsync("map-far@example.com", FreetownLat + 0.18, FreetownLng);
        var nearReportId = await CreateAndApproveReportAsync("map-near@example.com", FreetownLat, FreetownLng);

        var incidents = await GetNearbyAsync(radiusM: 1000);

        incidents.Should().Contain(i => i.Id == nearReportId);
        incidents.Should().NotContain(i => i.Id == farReportId);
    }

    [Fact]
    public async Task GetNearby_FiltersByCategoryWhenRequested()
    {
        var roadReportId = await CreateAndApproveReportAsync("map-road@example.com", FreetownLat, FreetownLng, _categoryId);
        var floodReportId = await CreateAndApproveReportAsync("map-flood@example.com", FreetownLat, FreetownLng, _otherCategoryId);

        var incidents = await GetNearbyAsync(categories: _categoryId.ToString());

        incidents.Should().Contain(i => i.Id == roadReportId);
        incidents.Should().NotContain(i => i.Id == floodReportId);
    }
}
