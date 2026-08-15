using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

public record MobileCategoryDto(
    Guid Id, string Name, string? Slug, string? IconKey, string? ColourToken, IncidentPriority DefaultPriority, int DisplayOrder);
