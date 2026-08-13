using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reports.Dtos;

public record ChangeStatusRequest(CaseStatus NewStatus, string? Notes);
