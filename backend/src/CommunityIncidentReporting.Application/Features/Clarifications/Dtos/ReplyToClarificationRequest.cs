namespace CommunityIncidentReporting.Application.Features.Clarifications.Dtos;

/// <summary>AttachmentId, if supplied, must already exist on the underlying report (see IClarificationService.ReplyAsync).</summary>
public record ReplyToClarificationRequest(string Message, Guid? AttachmentId);
