namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

public record RegisterReporterRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword,
    bool ConsentAccepted);
