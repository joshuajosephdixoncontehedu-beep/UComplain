using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

public record ConsentGrantItem(ConsentType ConsentType, bool Granted, string PolicyVersion);
