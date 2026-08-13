using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reports.Dtos;

public record UpdateReportRequest(Guid CategoryId, IncidentPriority Priority, string LocationDescription, string Description);
