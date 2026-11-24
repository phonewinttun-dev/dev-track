using DevTrack.Shared;
using DevTrack.Features.Access.Auth;

namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            #region Acquire API client

            var clientResult = CreateAuthClient();

            if (clientResult.IsFailure)
            {
                logger.LogInformation(
                    "LoginAsync => Acquire API client stopped. Reason={Reason}, NextAction=return failure",
                    clientResult.Message);

                return Result<LoginResponse>.Failure(clientResult.Message, clientResult.ErrorCode);
            }

            using var client = clientResult.Data!;

            logger.LogInformation(
                "LoginAsync => Client acquired, Reason={Reason}, NextAction=send request",
                clientResult.Message);

            #endregion

            #region Send API request

            var responseResult = await SendLoginRequestAsync(client, request, cancellationToken);

            if (responseResult.IsFailure)
            {
                logger.LogInformation(
                    "LoginAsync => Send API request stopped. Reason={Reason}, NextAction=return failure",
                    responseResult.Message);

                return Result<LoginResponse>.Failure(responseResult.Message, responseResult.ErrorCode);
            }

            using var response = responseResult.Data!;

            logger.LogInformation(
                "LoginAsync => API response received, StatusCode={StatusCode}, Reason={Reason}, NextAction=check response status",
                (int)response.StatusCode,
                responseResult.Message);

            #endregion

            #region Check API response

            var statusResult = CheckApiResponse(response);

            if (statusResult.IsFailure)
            {
                logger.LogInformation(
                    "LoginAsync => Check API response stopped. Reason={Reason}, NextAction=return unauthorized failure",
                    statusResult.Message);

                return Result<LoginResponse>.Failure(statusResult.Message, statusResult.ErrorCode);
            }

            logger.LogInformation(
                "LoginAsync => Response status accepted, Reason={Reason}, NextAction=read typed payload",
                statusResult.Message);

            #endregion

            #region Read API payload

            var payloadResult = await ReadApiPayloadAsync<LoginResponse>(
                response,
                "The API returned an empty login response.",
                cancellationToken);

            if (payloadResult.IsFailure)
            {
                logger.LogInformation(
                    "LoginAsync => Read API payload stopped. Reason={Reason}, NextAction=return failure",
                    payloadResult.Message);

                return Result<LoginResponse>.Failure(payloadResult.Message, payloadResult.ErrorCode);
            }

            logger.LogInformation(
                "LoginAsync => Payload read, Reason={Reason}, NextAction=return payload",
                payloadResult.Message);

            #endregion

            return payloadResult;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "LoginAsync => Authentication API operation failed.");

            throw;
        }
    }
}
