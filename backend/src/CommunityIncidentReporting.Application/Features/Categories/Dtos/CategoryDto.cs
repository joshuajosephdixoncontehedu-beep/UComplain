using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Categories.Dtos;

public record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    IncidentPriority DefaultPriority,
    int SlaHours,
    bool IsActive,
    int DisplayOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    // Mobile-app catalogue display fields — null until an admin sets them.
    string? Slug,
    string? IconKey,
    string? ColourToken);
