using DevTrack.Features.Access.Auth;
using DevTrack.Shared;

namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    private static async Task<Result<HttpResponseMessage>> SendLoginRequestAsync(
        HttpClient client,
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        #region Send the existing API request

        var response = await client.PostAsJsonAsync("api/auth/login", request, cancellationToken);

        return Result<HttpResponseMessage>.Success(response, "API request completed.");

        #endregion
    }
}
