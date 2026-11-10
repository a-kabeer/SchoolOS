using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SchoolOS.BuildingBlocks.Security;

/// <summary>
/// Cryptographic helper for generating and verifying HMAC-SHA256 JWT tokens.
/// Used for testing, authentication tokens, and service-to-service calls.
/// </summary>
public static class JwtTokenHelper
{
    public static string GenerateToken(
        JwtOptions options,
        string userId,
        Guid? tenantId = null,
        Guid? campusId = null,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? scopes = null,
        TimeSpan? lifetime = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (tenantId.HasValue)
        {
            claims.Add(new Claim(TenantContext.TenantIdClaimType, tenantId.Value.ToString()));
        }

        if (campusId.HasValue)
        {
            claims.Add(new Claim(TenantContext.CampusIdClaimType, campusId.Value.ToString()));
        }

        if (roles is not null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }
        }

        if (scopes is not null)
        {
            foreach (var scope in scopes)
            {
                claims.Add(new Claim(TenantContext.ScopeClaimType, scope));
            }
        }

        var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(options.ExpiryMinutes));
        var notBefore = expires < DateTime.UtcNow ? expires.AddMinutes(-5) : DateTime.UtcNow.AddSeconds(-5);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
