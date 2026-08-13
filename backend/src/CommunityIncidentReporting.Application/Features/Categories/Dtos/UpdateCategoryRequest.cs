using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Categories.Dtos;

public record UpdateCategoryRequest(
    string Name,
    string Description,
    IncidentPriority DefaultPriority,
    int SlaHours,
    int DisplayOrder);
