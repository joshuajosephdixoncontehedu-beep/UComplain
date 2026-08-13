using CommunityIncidentReporting.Application.Features.Auth.Dtos;

namespace CommunityIncidentReporting.Application.Features.Auth;

public interface IAuthService
{
    /// <summary>Throws InvalidCredentialsException on invalid credentials or an inactive account.</summary>
    Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>Throws InvalidCredentialsException if the refresh token is unknown, expired, or already revoked.</summary>
    Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    /// <summary>Revokes the given refresh token. No-ops (does not throw) if it is unknown or already revoked.</summary>
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task<AdminProfileDto> GetProfileAsync(Guid adminUserId, CancellationToken cancellationToken);
}
