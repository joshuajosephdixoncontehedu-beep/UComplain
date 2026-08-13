using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Administrators.Dtos;

public record CreateAdministratorRequest(string FullName, string Email, AdminRole Role, string TemporaryPassword);
