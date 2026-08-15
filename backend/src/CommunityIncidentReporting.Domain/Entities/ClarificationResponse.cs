namespace CommunityIncidentReporting.Domain.Entities;

/// <summary>A reporter's reply to one ClarificationRequest — several may accumulate per request (back-and-forth).</summary>
public class ClarificationResponse
{
    public Guid Id { get; set; }

    public Guid ClarificationRequestId { get; set; }
    public ClarificationRequest? ClarificationRequest { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid? AttachmentId { get; set; }
    public IncidentMediaAttachment? Attachment { get; set; }

    public DateTimeOffset RespondedAt { get; set; }
}
