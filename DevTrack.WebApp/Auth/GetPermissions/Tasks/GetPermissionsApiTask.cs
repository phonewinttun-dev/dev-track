using DevTrack.Shared;

namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    private static async Task<Result<HttpResponseMessage>> SendPermissionsRequestAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        #region Send the existing API request

        var response = await client.GetAsync("api/auth/permissions", cancellationToken);

        return Result<HttpResponseMessage>.Success(response, "API request completed.");

        #endregion
    }
}
