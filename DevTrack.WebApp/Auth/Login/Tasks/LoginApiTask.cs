using DevTrack.Features.Access.Auth;

namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    private static async Task<HttpResponseMessage> SendLoginRequestAsync(
        HttpClient client,
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        #region Send the existing API request

        var response = await client.PostAsJsonAsync("api/auth/login", request, cancellationToken);

        return response;

        #endregion
    }
}
