using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevTrack.Shared.Security;
using DevTrack.Api.Auth;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace DevTrack.Domain.Tests;

public class AdminJwtTokenServiceTests
{
    [Fact]
    public void CreateAdminToken_IncludesAdminIdentityAndValidatesWithSigningKey()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = AuthTestSettings.Issuer,
            Audience = AuthTestSettings.Audience,
            SigningKey = AuthTestSettings.SigningKey,
            ExpiresMinutes = 480
        }), NullLogger<JwtTokenService>.Instance);

        var tokenResult = service.CreateAdminToken(AuthTestSettings.Username, AuthTestSettings.DisplayName);

        Assert.True(tokenResult.IsSuccess);
        Assert.NotNull(tokenResult.Data);
        var token = tokenResult.Data;

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = AuthTestSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = AuthTestSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthTestSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out var validatedToken);

        Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal(AuthTestSettings.DisplayName, principal.Identity?.Name);
        Assert.True(principal.IsInRole(AuthConstants.AdminRole));
        Assert.Equal(AuthTestSettings.Username, principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}
