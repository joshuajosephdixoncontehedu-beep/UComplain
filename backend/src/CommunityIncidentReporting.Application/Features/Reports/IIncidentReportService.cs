using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Reports.Dtos;

namespace CommunityIncidentReporting.Application.Features.Reports;

/// <summary>
/// The operational report queue — always scoped to VerificationStatus == Verified.
/// Reports pending/needing clarification/suspected-duplicate/flagged/rejected never
/// appear here; they live in the verification queue (see IVerificationService) until
/// an authorized human decision moves them out.
/// </summary>
public interface IIncidentReportService
{
    Task<PagedResult<IncidentReportListItemDto>> GetAllAsync(GetReportsQuery query, CancellationToken cancellationToken);

    Task<IncidentReportDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IncidentReportDetailDto> UpdateAsync(
        Guid id, UpdateReportRequest request, RequestContext context, CancellationToken cancellationToken);

    /// <summary>Throws NotFoundException if the target admin doesn't exist.</summary>
    Task<IncidentReportDetailDto> AssignAsync(
        Guid id, AssignReportRequest request, RequestContext context, CancellationToken cancellationToken);

    Task<IncidentReportDetailDto> AddNoteAsync(
        Guid id, AddNoteRequest request, RequestContext context, CancellationToken cancellationToken);

    /// <summary>Throws BusinessRuleException for an illegal CaseStatus transition.</summary>
    Task<IncidentReportDetailDto> ChangeStatusAsync(
        Guid id, ChangeStatusRequest request, RequestContext context, CancellationToken cancellationToken);
}
