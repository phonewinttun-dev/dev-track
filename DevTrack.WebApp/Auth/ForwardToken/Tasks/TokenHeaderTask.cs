using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace DevTrack.WebApp.Auth;

public partial class TokenSendHandler
{
    private async Task<string?> ReadAccessTokenAsync()
    {
        #region Read the optional credential from the protected cookie

        var context = httpContextAccessor.HttpContext;
        return context is null ? null : await context.GetTokenAsync("access_token");

        #endregion
    }

    private static AuthenticationHeaderValue? PrepareAuthorizationHeader(string? token)
    {
        #region Preserve an existing header when no stored token is available

        return string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);

        #endregion
    }
}
