namespace CommunityIncidentReporting.Application.Features.Auth.Dtos;

public record AdminProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt);
