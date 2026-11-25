using DevTrack.Features.Access.Auth;
using DevTrack.Shared;
using DevTrack.WebApp.Services;

namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            #region Send the request and validate the response

            using var client = clientFactory.CreateClient(ApiClientBase.ClientName);
            using var response = await SendLoginRequestAsync(client, request, cancellationToken);
            var result = await ReadAuthResponseAsync<LoginResponse>(
                response, "The API returned an empty login response.", cancellationToken);

            logger.LogInformation("LoginAsync => API request completed. StatusCode={StatusCode}, IsSuccess={IsSuccess}",
                (int)response.StatusCode, result.IsSuccess);

            return result;

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "LoginAsync => Authentication API operation failed.");
            throw;
        }
    }
}
