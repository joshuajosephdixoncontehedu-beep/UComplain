namespace CommunityIncidentReporting.Application.Features.MobileReports.Dtos;

public record SignedUrlResponse(string Url, DateTimeOffset ExpiresAt);
