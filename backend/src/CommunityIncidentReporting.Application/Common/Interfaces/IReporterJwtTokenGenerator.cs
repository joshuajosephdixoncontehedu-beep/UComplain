using CommunityIncidentReporting.Domain.Entities;

namespace CommunityIncidentReporting.Application.Common.Interfaces;

/// <summary>
/// Separate from IJwtTokenGenerator (AdminUser-only) deliberately — reporter tokens are
/// signed with their own secret/audience so the two token types can never be confused or
/// forged from one another. See ReporterJwtOptions.
/// </summary>
public interface IReporterJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(Reporter reporter);
}
