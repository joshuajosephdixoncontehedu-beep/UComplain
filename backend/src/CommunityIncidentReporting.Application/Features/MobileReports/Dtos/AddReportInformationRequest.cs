namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

/// <summary>
/// AttachmentId, if supplied, must already exist on the same report (uploaded earlier,
/// while the report was still in a mutable case status — see MediaAttachmentService).
/// This never accepts a new file upload; it can only reference something already there.
/// </summary>
public record AddReportInformationRequest(string Message, Guid? AttachmentId);
