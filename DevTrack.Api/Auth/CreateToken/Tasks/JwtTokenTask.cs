using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevTrack.Shared;
using Microsoft.IdentityModel.Tokens;

namespace DevTrack.Api.Auth;

public partial class JwtTokenService
{
    private Result<SigningCredentials> PrepareSigningCredentials()
    {
        #region Prepare the existing signing algorithm

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        return Result<SigningCredentials>.Success(credentials, "Signing credentials prepared.");

        #endregion
    }

    private Result<JwtSecurityToken> BuildToken(
        IEnumerable<Claim> claims,
        SigningCredentials credentials,
        DateTime issuedAt)
    {
        #region Build the existing claims and lifetime

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: issuedAt.AddMinutes(jwtOptions.ExpiresMinutes),
            signingCredentials: credentials);

        return Result<JwtSecurityToken>.Success(token, "Token claims and lifetime prepared.");

        #endregion
    }

    private static Result<string> SerializeToken(JwtSecurityToken token)
    {
        #region Serialize the signed token

        var serialized = new JwtSecurityTokenHandler().WriteToken(token);

        return Result<string>.Success(serialized, "Token serialized.");

        #endregion
    }
}
