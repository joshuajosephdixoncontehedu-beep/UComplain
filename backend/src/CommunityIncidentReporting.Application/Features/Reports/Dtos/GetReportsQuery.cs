using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reports.Dtos;

public record GetReportsQuery : PagedRequest
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public IncidentPriority? Priority { get; init; }
    public CaseStatus? CaseStatus { get; init; }
    public Guid? AssignedAdminId { get; init; }
    public string? Location { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public string? SortBy { get; init; }
    public string? SortDir { get; init; }
}
