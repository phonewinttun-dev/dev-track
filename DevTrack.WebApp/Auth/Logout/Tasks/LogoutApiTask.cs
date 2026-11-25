namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    private static async Task<HttpResponseMessage> SendLogoutRequestAsync(
        HttpClient client, CancellationToken cancellationToken)
    {
        #region Send the revocation request

        var response = await client.PostAsync("api/auth/logout", null, cancellationToken);
        return response;

        #endregion
    }
}
