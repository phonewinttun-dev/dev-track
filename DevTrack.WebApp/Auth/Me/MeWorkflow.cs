using DevTrack.Features.Access.Auth;
using DevTrack.Shared;
using DevTrack.Shared.Security;
using DevTrack.WebApp.Services;

namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    public async Task<Result<MeResponse>> GetMeAsync(CancellationToken cancellationToken)
    {
        try
        {
            #region Request the current user and validate the response

            using var client = clientFactory.CreateClient(ApiClientBase.ClientName);
            using var response = await SendMeRequestAsync(client, cancellationToken);
            var result = await ReadAuthResponseAsync<MeResponse>(
                response, "The API returned an empty current user response.", cancellationToken);

            if (result.IsSuccess && !IsValidUser(result.Data!))
            {
                result = Result<MeResponse>.Failure("The API returned invalid current user details.",
                    AuthErrorCodes.InvalidResponse);
            }

            logger.LogInformation("GetMeAsync => Current user request completed. StatusCode={StatusCode}, IsSuccess={IsSuccess}",
                (int)response.StatusCode, result.IsSuccess);

            return result;

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetMeAsync => Current user request failed.");
            throw;
        }
    }
}
