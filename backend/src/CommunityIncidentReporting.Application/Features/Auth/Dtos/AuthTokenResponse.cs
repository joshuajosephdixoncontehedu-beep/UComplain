namespace CommunityIncidentReporting.Application.Features.Auth.Dtos;

public record AuthTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AdminProfileDto Admin);
