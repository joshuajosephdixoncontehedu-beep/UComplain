using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Administrators.Dtos;

public record UpdateAdministratorRequest(string FullName, string Email, AdminRole Role);
