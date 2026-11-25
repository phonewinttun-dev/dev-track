using DevTrack.Shared;
using DevTrack.WebApp.Services;

namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    public async Task<Result> LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            #region Revoke the API session

            using var client = clientFactory.CreateClient(ApiClientBase.ClientName);
            using var response = await SendLogoutRequestAsync(client, cancellationToken);
            var result = CheckApiResponse(response);

            logger.LogInformation("LogoutAsync => API sign out completed. StatusCode={StatusCode}, IsSuccess={IsSuccess}",
                (int)response.StatusCode, result.IsSuccess);

            return result;

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "LogoutAsync => API sign out failed.");
            throw;
        }
    }
}
