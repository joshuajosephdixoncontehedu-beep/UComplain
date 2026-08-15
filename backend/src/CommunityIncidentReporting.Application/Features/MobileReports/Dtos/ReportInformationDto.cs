namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

public record ReportInformationDto(Guid Id, string Message, Guid? AttachmentId, DateTimeOffset CreatedAt);
