using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

public record ConsentDto(Guid Id, ConsentType ConsentType, bool Granted, string PolicyVersion, DateTimeOffset GrantedAt);
