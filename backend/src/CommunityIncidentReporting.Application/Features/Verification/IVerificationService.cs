using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using CommunityIncidentReporting.Application.Features.Verification.Dtos;

namespace CommunityIncidentReporting.Application.Features.Verification;

/// <summary>
/// Reports here are NOT active operational incidents — they haven't passed
/// verification yet. No automated "false report" judgment is ever made; every
/// decision requires an authorized human action, recorded with who/when/why.
/// </summary>
public interface IVerificationService
{
    Task<VerificationQueueResponse> GetQueueAsync(CancellationToken cancellationToken);

    /// <summary>Throws BusinessRuleException if the report is already Verified.</summary>
    Task<IncidentReportDetailDto> DecideAsync(
        Guid id, VerificationDecisionRequest request, RequestContext context, CancellationToken cancellationToken);
}
