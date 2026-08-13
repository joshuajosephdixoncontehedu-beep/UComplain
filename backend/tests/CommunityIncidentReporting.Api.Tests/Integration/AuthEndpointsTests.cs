using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIncidentReporting.Api.Tests.Integration;

// Deliberately not IClassFixture-shared: each test gets its own factory instance (and
// therefore its own isolated InMemory database), so tests can't see each other's data
// or leak duplicate seed rows across test methods.
public class AuthEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private const string Password = "Correct-Horse-Battery-Staple-1";

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();

        await using var db = _factory.CreateDbContext();
        db.AdminUsers.AddRange(
            SeedAdmin("superadmin@cirs.gov.sl", AdminRole.SuperAdmin),
            SeedAdmin("manager@cirs.gov.sl", AdminRole.IncidentManager),
            SeedAdmin("reviewer@cirs.gov.sl", AdminRole.Reviewer),
            SeedAdmin("analyst@cirs.gov.sl", AdminRole.ReadOnlyAnalyst));
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

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndProfile()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login",
            new LoginRequest("superadmin@cirs.gov.sl", Password));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Admin.Role.Should().Be(nameof(AdminRole.SuperAdmin));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401WithEnvelope()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login",
            new LoginRequest("superadmin@cirs.gov.sl", "wrong-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("invalid_credentials");
    }

    [Fact]
    public async Task Login_WithMissingPassword_Returns422ValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/auth/login",
            new { Email = "superadmin@cirs.gov.sl", Password = "" });

        response.StatusCode.Should().Be((HttpStatusCode)422);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("validation_error");
    }

    [Fact]
    public async Task Me_WithoutAToken_Returns401()
    {
        var response = await _client.GetAsync("/api/admin/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithAGarbageToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var response = await _client.GetAsync("/api/admin/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithAValidToken_ReturnsTheMatchingProfile()
    {
        var login = await _client.PostAsJsonAsync("/api/admin/auth/login",
            new LoginRequest("reviewer@cirs.gov.sl", Password));
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var response = await _client.GetAsync("/api/admin/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<AdminProfileDto>();
        profile!.Email.Should().Be("reviewer@cirs.gov.sl");
        profile.Role.Should().Be(nameof(AdminRole.Reviewer));
    }

    [Fact]
    public async Task RefreshThenReusingTheOldToken_Returns401()
    {
        var login = await _client.PostAsJsonAsync("/api/admin/auth/login",
            new LoginRequest("manager@cirs.gov.sl", Password));
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokenResponse>();

        var refreshed = await _client.PostAsJsonAsync("/api/admin/auth/refresh",
            new RefreshTokenRequest(tokens!.RefreshToken));
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);

        var reuse = await _client.PostAsJsonAsync("/api/admin/auth/refresh",
            new RefreshTokenRequest(tokens.RefreshToken));
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ThenRefreshingWithTheSameToken_Returns401()
    {
        var login = await _client.PostAsJsonAsync("/api/admin/auth/login",
            new LoginRequest("analyst@cirs.gov.sl", Password));
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var logout = await _client.PostAsJsonAsync("/api/admin/auth/logout", new RefreshTokenRequest(tokens.RefreshToken));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        _client.DefaultRequestHeaders.Authorization = null;
        var refresh = await _client.PostAsJsonAsync("/api/admin/auth/refresh", new RefreshTokenRequest(tokens.RefreshToken));
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(Policies.SuperAdminOnly, new[] { "SuperAdmin" })]
    [InlineData(Policies.ManagerOrAbove, new[] { "SuperAdmin", "IncidentManager" })]
    [InlineData(Policies.ReviewerOrAbove, new[] { "SuperAdmin", "IncidentManager", "Reviewer" })]
    public async Task NamedPolicy_RequiresExactlyTheExpectedRoles(string policyName, string[] expectedRoles)
    {
        using var scope = _factory.Services.CreateScope();
        var policyProvider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync(policyName);

        policy.Should().NotBeNull();
        var rolesRequirement = policy!.Requirements.OfType<RolesAuthorizationRequirement>().Single();
        rolesRequirement.AllowedRoles.Should().BeEquivalentTo(expectedRoles);
    }
}
