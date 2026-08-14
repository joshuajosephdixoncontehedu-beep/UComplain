using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.Security;
using CommunityIncidentReporting.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Api.Tests.Services;

public class AuthServiceTests
{
    private static readonly JwtOptions TestJwtOptions = new()
    {
        Secret = "unit-test-signing-secret-at-least-32-characters-long",
        Issuer = "UComplain.Tests",
        Audience = "UComplain.Tests.AdminPortal",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AuthService CreateSut(AppDbContext db) => new(
        db,
        new BCryptPasswordHasher(),
        new JwtTokenGenerator(Options.Create(TestJwtOptions)),
        Options.Create(TestJwtOptions));

    private static async Task<AdminUser> SeedAdminAsync(
        AppDbContext db, string password, AdminRole role = AdminRole.IncidentManager, bool isActive = true)
    {
        var admin = new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = "Test Admin",
            Email = "test.admin@cirs.gov.sl",
            PasswordHash = new BCryptPasswordHasher().Hash(password),
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();
        return admin;
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsTokenPairAndProfile()
    {
        await using var db = CreateContext();
        var admin = await SeedAdminAsync(db, "correct-password-1");
        var sut = CreateSut(db);

        var result = await sut.LoginAsync(new LoginRequest(admin.Email, "correct-password-1"), CancellationToken.None);

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Admin.Email.Should().Be(admin.Email);
        result.Admin.Role.Should().Be(nameof(AdminRole.IncidentManager));
    }

    [Fact]
    public async Task LoginAsync_IsCaseInsensitiveOnEmail()
    {
        await using var db = CreateContext();
        var admin = await SeedAdminAsync(db, "correct-password-1");
        var sut = CreateSut(db);

        var result = await sut.LoginAsync(
            new LoginRequest(admin.Email.ToUpperInvariant(), "correct-password-1"), CancellationToken.None);

        result.Admin.Email.Should().Be(admin.Email);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentials()
    {
        await using var db = CreateContext();
        var admin = await SeedAdminAsync(db, "correct-password-1");
        var sut = CreateSut(db);

        var act = () => sut.LoginAsync(new LoginRequest(admin.Email, "wrong-password"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsInvalidCredentials()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);

        var act = () => sut.LoginAsync(
            new LoginRequest("nobody@cirs.gov.sl", "whatever-password"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_ForADeactivatedAdmin_ThrowsInvalidCredentials()
    {
        await using var db = CreateContext();
        var admin = await SeedAdminAsync(db, "correct-password-1", isActive: false);
        var sut = CreateSut(db);

        var act = () => sut.LoginAsync(new LoginRequest(admin.Email, "correct-password-1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task RefreshAsync_WithAValidToken_RotatesItAndReturnsANewPair()
    {
        await using var db = CreateContext();
        var admin = await SeedAdminAsync(db, "correct-password-1");
        var sut = CreateSut(db);
        var login = await sut.LoginAsync(new LoginRequest(admin.Email, "correct-password-1"), CancellationToken.None);

        var refreshed = await sut.RefreshAsync(new RefreshTokenRequest(login.RefreshToken), CancellationToken.None);

        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);
        refreshed.AccessToken.Should().NotBeNullOrWhiteSpace();

        var oldTokenHash = RefreshTokenGenerator.Hash(login.RefreshToken);
        var oldTokenRow = await db.RefreshTokens.FirstAsync(t => t.TokenHash == oldTokenHash);
        oldTokenRow.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshAsync_WithAnAlreadyRotatedToken_ThrowsInvalidCredentials()
    {
        await using var db = CreateContext();
        var admin = await SeedAdminAsync(db, "correct-password-1");
        var sut = CreateSut(db);
        var login = await sut.LoginAsync(new LoginRequest(admin.Email, "correct-password-1"), CancellationToken.None);
        await sut.RefreshAsync(new RefreshTokenRequest(login.RefreshToken), CancellationToken.None);

        var act = () => sut.RefreshAsync(new RefreshTokenRequest(login.RefreshToken), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>("the original token was revoked when it was rotated");
    }

    [Fact]
    public async Task RefreshAsync_WithAnUnknownToken_ThrowsInvalidCredentials()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);

        var act = () => sut.RefreshAsync(new RefreshTokenRequest("not-a-real-token"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task RefreshAsync_WithAnExpiredToken_ThrowsInvalidCredentials()
    {
        await using var db = CreateContext();
        var admin = await SeedAdminAsync(db, "correct-password-1");
        var raw = RefreshTokenGenerator.GenerateRawToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            AdminUserId = admin.Id,
            TokenHash = RefreshTokenGenerator.Hash(raw),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-8)
        });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var act = () => sut.RefreshAsync(new RefreshTokenRequest(raw), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LogoutAsync_RevokesAnActiveToken()
    {
        await using var db = CreateContext();
        var admin = await SeedAdminAsync(db, "correct-password-1");
        var sut = CreateSut(db);
        var login = await sut.LoginAsync(new LoginRequest(admin.Email, "correct-password-1"), CancellationToken.None);

        await sut.LogoutAsync(new RefreshTokenRequest(login.RefreshToken), CancellationToken.None);

        var act = () => sut.RefreshAsync(new RefreshTokenRequest(login.RefreshToken), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LogoutAsync_WithAnUnknownToken_DoesNotThrow()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);

        var act = () => sut.LogoutAsync(new RefreshTokenRequest("not-a-real-token"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetProfileAsync_ForAnExistingAdmin_ReturnsTheProfile()
    {
        await using var db = CreateContext();
        var admin = await SeedAdminAsync(db, "correct-password-1", role: AdminRole.SuperAdmin);
        var sut = CreateSut(db);

        var profile = await sut.GetProfileAsync(admin.Id, CancellationToken.None);

        profile.Email.Should().Be(admin.Email);
        profile.Role.Should().Be(nameof(AdminRole.SuperAdmin));
    }

    [Fact]
    public async Task GetProfileAsync_ForAnUnknownId_ThrowsNotFound()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);

        var act = () => sut.GetProfileAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
