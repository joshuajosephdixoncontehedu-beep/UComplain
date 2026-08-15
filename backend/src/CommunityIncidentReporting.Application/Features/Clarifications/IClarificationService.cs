using CommunityIncidentReporting.Application.Features.Clarifications.Dtos;

namespace CommunityIncidentReporting.Application.Features.Clarifications;

/// <summary>
/// Reporter-facing side of the clarification loop. The admin-facing side — creating a
/// ClarificationRequest — happens inline inside VerificationService.DecideAsync's
/// RequestClarification branch, not here.
/// </summary>
public interface IClarificationService
{
    /// <summary>Throws NotFoundException if the report doesn't exist or isn't owned by reporterId.</summary>
    Task<IReadOnlyList<ClarificationRequestDto>> GetForReportAsync(
        Guid reportId, Guid reporterId, CancellationToken cancellationToken);

    /// <summary>
    /// Throws NotFoundException if the clarification request doesn't exist or its report
    /// isn't owned by reporterId (indistinguishable), BusinessRuleException if the report
    /// has already moved past NeedsClarification (re-decided or auto-closed elsewhere),
    /// and NotFoundException if AttachmentId is supplied but isn't an existing,
    /// non-deleted attachment on the same report.
    /// </summary>
    Task<ClarificationResponseDto> ReplyAsync(
        Guid clarificationRequestId, Guid reporterId, ReplyToClarificationRequest request,
        string? requestIp, string? userAgent, CancellationToken cancellationToken);
}
