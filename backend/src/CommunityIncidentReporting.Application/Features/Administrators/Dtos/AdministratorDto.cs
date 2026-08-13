using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Administrators.Dtos;

public record AdministratorDto(
    Guid Id,
    string FullName,
    string Email,
    AdminRole Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);
