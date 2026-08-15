using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

public record DraftDto(
    Guid Id,
    Guid? CategoryId,
    string? CategoryName,
    string? Description,
    DateTimeOffset? IncidentOccurredAt,
    IncidentPriority? InitialPrioritySignal,
    string? LocationDescription,
    double? Latitude,
    double? Longitude,
    string? Landmark,
    Guid? SubmittedReportId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<MediaAttachmentDto> Attachments);
