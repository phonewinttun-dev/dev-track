using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace DevTrack.Features.Access.Auth;

public partial class SessionService
{
    private const int SessionIdSize = 32;
    private const int EncodedSessionIdLength = 43;
    private const int MaximumTokenLength = 2048;
    private const string SessionIdClaim = "sid";

    private string GenerateToken()
    {
        #region Sign a JWT containing only a random session identifier

        var sessionId = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(SessionIdSize));
        var credentials = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(new JwtHeader(credentials), new JwtPayload
        {
            [SessionIdClaim] = sessionId
        });
        return new JwtSecurityTokenHandler().WriteToken(token);

        #endregion
    }

    private bool IsValidToken(string token)
    {
        #region Validate the signature and the single allowed claim

        if (string.IsNullOrWhiteSpace(token) || token.Length > MaximumTokenLength)
        {
            return false;
        }

        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(token))
        {
            return false;
        }

        try
        {
            // This session JWT has no identity, issuer, audience, or lifetime claims.
            // The database lookup enforces the exact token, expiration, revocation, and active user.
            handler.ValidateToken(token, new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = GetSigningKey(),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidTypes = ["JWT"],
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireAudience = false,
                ValidateLifetime = false,
                RequireExpirationTime = false
            }, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwt || jwt.Payload.Count != 1
                || !jwt.Payload.TryGetValue(SessionIdClaim, out var value) || value is not string sessionId)
            {
                return false;
            }

            return sessionId.Length == EncodedSessionIdLength && sessionId.All(character =>
                char.IsAsciiLetterOrDigit(character) || character == '-' || character == '_');
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        #endregion
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        #region Use the server's configured signing key

        return new SymmetricSecurityKey(Convert.FromBase64String(options.Value.SigningKey));

        #endregion
    }
}
