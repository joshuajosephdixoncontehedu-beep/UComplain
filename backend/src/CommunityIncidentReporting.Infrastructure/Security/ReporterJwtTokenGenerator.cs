using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CommunityIncidentReporting.Infrastructure.Security;

public class ReporterJwtTokenGenerator(IOptions<ReporterJwtOptions> options) : IReporterJwtTokenGenerator
{
    /// <summary>
    /// Role claim value stamped on every reporter token — checked by Policies.RequireReporter
    /// in addition to the "ReporterScheme" audience/signing-key check, as defense in depth.
    /// </summary>
    public const string ReporterRoleClaimValue = "Reporter";

    public (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(Reporter reporter)
    {
        var opts = options.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(opts.ReporterAccessTokenMinutes);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, reporter.Id.ToString()),
            new(ClaimTypes.NameIdentifier, reporter.Id.ToString()),
            new(ClaimTypes.Email, reporter.Email ?? string.Empty),
            new(ClaimTypes.Name, reporter.FullName ?? string.Empty),
            new(ClaimTypes.Role, ReporterRoleClaimValue),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.ReporterSecret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: opts.ReporterIssuer,
            audience: opts.ReporterAudience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
