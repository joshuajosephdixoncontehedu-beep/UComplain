using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CommunityIncidentReporting.Infrastructure.Security;

public class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    public (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(AdminUser admin)
    {
        var opts = options.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(opts.AccessTokenMinutes);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Email, admin.Email),
            new(ClaimTypes.Name, admin.FullName),
            new(ClaimTypes.Role, admin.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: opts.Issuer,
            audience: opts.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
