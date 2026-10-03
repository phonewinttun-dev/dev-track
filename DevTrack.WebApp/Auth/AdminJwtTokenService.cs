using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevTrack.Shared.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DevTrack.WebApp.Auth;

public class AdminJwtTokenService : IAdminJwtTokenService
{
    private readonly JwtOptions _options;

    public AdminJwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateAdminToken(string username, string displayName)
    {
        return CreateToken([new(ClaimTypes.NameIdentifier, username), new(ClaimTypes.Name, displayName), new(ClaimTypes.Role, AuthConstants.AdminRole)]);
    }

    public string CreateToken(IEnumerable<Claim> claims)
    {
        var now = DateTime.UtcNow;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_options.ExpiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
