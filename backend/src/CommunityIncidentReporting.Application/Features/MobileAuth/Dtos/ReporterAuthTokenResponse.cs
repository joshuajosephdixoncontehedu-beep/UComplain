namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

public record ReporterAuthTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    ReporterProfileDto Reporter);
