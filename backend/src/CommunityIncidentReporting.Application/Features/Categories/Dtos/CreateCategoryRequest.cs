using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Categories.Dtos;

public record CreateCategoryRequest(
    string Name,
    string Description,
    IncidentPriority DefaultPriority,
    int SlaHours,
    int DisplayOrder);
