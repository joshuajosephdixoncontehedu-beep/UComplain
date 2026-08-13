using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Features.Auth;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class AuthService(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";
    private const string InvalidRefreshTokenMessage = "Invalid or expired refresh token.";

    public async Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var admin = await db.AdminUsers
            .FirstOrDefaultAsync(a => a.Email.ToLower() == normalizedEmail, cancellationToken);

        if (admin is null || !admin.IsActive || !passwordHasher.Verify(request.Password, admin.PasswordHash))
        {
            throw new InvalidCredentialsException(InvalidCredentialsMessage);
        }

        admin.LastLoginAt = DateTimeOffset.UtcNow;
        admin.UpdatedAt = DateTimeOffset.UtcNow;

        var response = await IssueTokenPairAsync(admin, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenGenerator.Hash(request.RefreshToken);
        var existingToken = await db.RefreshTokens
            .Include(t => t.AdminUser)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive || existingToken.AdminUser is null
            || !existingToken.AdminUser.IsActive)
        {
            throw new InvalidCredentialsException(InvalidRefreshTokenMessage);
        }

        var admin = existingToken.AdminUser;
        var response = await IssueTokenPairAsync(admin, cancellationToken, revoking: existingToken);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = RefreshTokenGenerator.Hash(request.RefreshToken);
        var existingToken = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken is { RevokedAt: null })
        {
            existingToken.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AdminProfileDto> GetProfileAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        var admin = await db.AdminUsers.FindAsync([adminUserId], cancellationToken)
            ?? throw new NotFoundException(nameof(AdminUser), adminUserId);

        return ToProfileDto(admin);
    }

    private async Task<AuthTokenResponse> IssueTokenPairAsync(
        AdminUser admin, CancellationToken cancellationToken, RefreshToken? revoking = null)
    {
        var (accessToken, accessExpiresAt) = jwtTokenGenerator.GenerateAccessToken(admin);

        var rawRefreshToken = RefreshTokenGenerator.GenerateRawToken();
        var refreshTokenHash = RefreshTokenGenerator.Hash(rawRefreshToken);
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays);

        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            AdminUserId = admin.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (revoking is not null)
        {
            revoking.RevokedAt = DateTimeOffset.UtcNow;
            revoking.ReplacedByTokenHash = refreshTokenHash;
        }

        db.RefreshTokens.Add(newToken);
        await Task.CompletedTask;

        return new AuthTokenResponse(accessToken, accessExpiresAt, rawRefreshToken, refreshExpiresAt, ToProfileDto(admin));
    }

    private static AdminProfileDto ToProfileDto(AdminUser admin) => new(
        admin.Id, admin.FullName, admin.Email, admin.Role.ToString(), admin.IsActive, admin.LastLoginAt);
}
