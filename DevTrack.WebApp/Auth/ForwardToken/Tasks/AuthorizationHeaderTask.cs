using System.Net.Http.Headers;
using DevTrack.Shared;
using Microsoft.AspNetCore.Authentication;
using Shared;

namespace DevTrack.WebApp.Auth;

public partial class AdminJwtAuthorizationHandler
{
    private async Task<Result<string?>> ReadAccessTokenAsync()
    {
        #region Read the optional stored token

        var context = httpContextAccessor.HttpContext;

        if (context is null)
        {
            return Result<string?>.Success(null, "No HTTP context is available.");
        }

        var token = await context.GetTokenAsync("access_token");

        return Result<string?>.Success(token, "Stored token lookup completed.");

        #endregion
    }

    private static Result<AuthenticationHeaderValue?> PrepareAuthorizationHeader(string? token)
    {
        #region Preserve existing headers when no token is stored

        if (token.IsNullOrEmpty())
        {
            return Result<AuthenticationHeaderValue?>.Success(null, "No stored token; existing header is unchanged.");
        }

        var header = new AuthenticationHeaderValue("Bearer", token);

        return Result<AuthenticationHeaderValue?>.Success(header, "Bearer header prepared.");

        #endregion
    }

    private async Task<Result<HttpResponseMessage>> ForwardRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        #region Send through the existing handler pipeline

        var response = await base.SendAsync(request, cancellationToken);

        return Result<HttpResponseMessage>.Success(response, "Request forwarded.");

        #endregion
    }
}
