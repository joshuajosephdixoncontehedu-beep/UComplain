using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.AuditLogs.Dtos;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Application.Features.Categories.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;

namespace CommunityIncidentReporting.Api.Tests.Integration;

// Regression coverage for two bugs found via manual browser testing in Phase 7:
// AuditLogsController resolving to the wrong route, and CategoryService allowing a
// raw database-constraint 500 instead of a clean business-rule error on a duplicate
// name. Also covers the general "every mutation writes an audit log" requirement,
// which wasn't directly asserted anywhere before this file.
public class CategoriesAndAuditLogTests : IAsyncLifetime
{
    // Matches the server's AddJsonOptions(JsonStringEnumConverter) in Program.cs.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private const string Password = "Correct-Horse-Battery-Staple-1";

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        await using var db = _factory.CreateDbContext();
        db.AdminUsers.AddRange(
            SeedAdmin("superadmin@cirs.gov.sl", AdminRole.SuperAdmin),
            SeedAdmin("manager@cirs.gov.sl", AdminRole.IncidentManager));
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static AdminUser SeedAdmin(string email, AdminRole role) => new()
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

    private async Task AuthenticateAsAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login", new LoginRequest(email, Password));
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
    }

    private static CreateCategoryRequest NewCategoryRequest(string name) =>
        new(name, "A test category.", IncidentPriority.Medium, 24, 0);

    [Fact]
    public async Task CreatingACategoryWithADuplicateName_ReturnsConflictNotA500()
    {
        await AuthenticateAsAsync("manager@cirs.gov.sl");

        var first = await _client.PostAsJsonAsync("/api/admin/categories", NewCategoryRequest("Road Traffic Accident"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await _client.PostAsJsonAsync("/api/admin/categories", NewCategoryRequest("Road Traffic Accident"));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await duplicate.Content.ReadAsStringAsync();
        body.Should().Contain("business_rule_violation");
        body.Should().NotContain("Npgsql", "a duplicate-name conflict must never leak a raw database exception");
    }

    [Fact]
    public async Task CreatingACategoryWithADuplicateName_IsCaseInsensitive()
    {
        await AuthenticateAsAsync("manager@cirs.gov.sl");

        var first = await _client.PostAsJsonAsync("/api/admin/categories", NewCategoryRequest("Flooding"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await _client.PostAsJsonAsync("/api/admin/categories", NewCategoryRequest("FLOODING"));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AuditLogsEndpoint_IsReachableAtItsDocumentedKebabCaseRoute()
    {
        // Regression test for the route resolving to /api/admin/AuditLogs (the default
        // [controller] token) instead of the documented /api/admin/audit-logs.
        await AuthenticateAsAsync("superadmin@cirs.gov.sl");

        var response = await _client.GetAsync("/api/admin/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuditLogsEndpoint_IsSuperAdminOnly()
    {
        await AuthenticateAsAsync("manager@cirs.gov.sl");

        var response = await _client.GetAsync("/api/admin/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatingACategory_WritesAnAuditLogEntryForThatAction()
    {
        await AuthenticateAsAsync("superadmin@cirs.gov.sl");

        var create = await _client.PostAsJsonAsync("/api/admin/categories", NewCategoryRequest("Power Outage"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);

        var auditResponse = await _client.GetAsync("/api/admin/audit-logs");
        var auditPage = await auditResponse.Content.ReadFromJsonAsync<PagedResult<AuditLogListItemDto>>();

        var entry = auditPage!.Items.Should().ContainSingle(e =>
            e.Action == "CategoryCreated" && e.EntityId == created!.Id.ToString()).Subject;
        entry.EntityType.Should().Be(nameof(IncidentCategory));
        entry.AdminUserName.Should().Be("Test SuperAdmin");
        entry.NewValueJson.Should().Contain("Power Outage");
    }

    [Fact]
    public async Task AuditLogsEndpoint_FiltersByEntityType()
    {
        await AuthenticateAsAsync("superadmin@cirs.gov.sl");
        await _client.PostAsJsonAsync("/api/admin/categories", NewCategoryRequest("Landslide"));

        var response = await _client.GetAsync($"/api/admin/audit-logs?entityType={nameof(IncidentCategory)}");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogListItemDto>>();

        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(e => e.EntityType == nameof(IncidentCategory));
    }
}
