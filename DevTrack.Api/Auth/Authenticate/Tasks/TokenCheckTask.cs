using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using DevTrack.Shared;

namespace DevTrack.Api.Auth;

public partial class TokenCheckHandler
{
    private Result<AuthenticationHeaderValue?> ReadBearerHeader()
    {
        #region Read the optional bearer header

        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Result<AuthenticationHeaderValue?>.Success(null);
        }

        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
            || !header.Scheme.Equals(SchemeName, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter))
        {
            return Result<AuthenticationHeaderValue?>.Failure("Invalid authorization header.");
        }

        return Result<AuthenticationHeaderValue?>.Success(header);

        #endregion
    }

    private static ClaimsPrincipal BuildPrincipal(int userId)
    {
        #region Keep the internal identity on the server

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture))], SchemeName);
        return new ClaimsPrincipal(identity);

        #endregion
    }
}
