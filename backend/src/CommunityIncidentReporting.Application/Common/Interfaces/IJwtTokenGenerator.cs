using CommunityIncidentReporting.Domain.Entities;

namespace CommunityIncidentReporting.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(AdminUser admin);
}
