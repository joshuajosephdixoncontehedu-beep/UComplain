namespace CommunityIncidentReporting.Application.Features.Reports.Dtos;

public record InternalNoteDto(
    Guid Id,
    string Content,
    Guid CreatedByAdminId,
    string CreatedByAdminName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
