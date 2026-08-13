using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Reporters.Dtos;

public record GetReportersQuery : PagedRequest
{
    public string? Search { get; init; }
    public VerificationStatus? VerificationStatus { get; init; }
    public bool? IsRestricted { get; init; }
}
