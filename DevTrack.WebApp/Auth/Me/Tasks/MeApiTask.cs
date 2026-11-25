using DevTrack.Features.Access.Auth;

namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    private static async Task<HttpResponseMessage> SendMeRequestAsync(
        HttpClient client, CancellationToken cancellationToken)
    {
        #region Load details from the authenticated API endpoint

        return await client.GetAsync("me", cancellationToken);

        #endregion
    }

    private static bool IsValidUser(MeResponse user)
    {
        #region Reject incomplete profile or permission data

        return user.UserId > 0
            && !string.IsNullOrWhiteSpace(user.Username)
            && user.DisplayName is not null
            && !string.IsNullOrWhiteSpace(user.Role)
            && user.Permissions is not null
            && user.Permissions.All(permission => !string.IsNullOrWhiteSpace(permission));

        #endregion
    }
}
