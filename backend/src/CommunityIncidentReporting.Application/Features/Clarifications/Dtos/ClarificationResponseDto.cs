namespace CommunityIncidentReporting.Application.Features.Clarifications.Dtos;

public record ClarificationResponseDto(Guid Id, string Message, Guid? AttachmentId, DateTimeOffset RespondedAt);
