namespace CommunityIncidentReporting.Application.Features.Verification.Dtos;

public record VerificationQueueResponse(
    IReadOnlyList<VerificationQueueItemDto> Pending,
    IReadOnlyList<VerificationQueueItemDto> NeedsClarification,
    IReadOnlyList<VerificationQueueItemDto> SuspectedDuplicate,
    IReadOnlyList<VerificationQueueItemDto> FlaggedAbuse,
    IReadOnlyList<VerificationQueueItemDto> Rejected);
