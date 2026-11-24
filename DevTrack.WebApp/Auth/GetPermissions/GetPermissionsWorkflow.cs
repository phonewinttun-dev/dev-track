using DevTrack.Shared;
namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    public async Task<Result<string[]>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            #region Acquire API client

            var clientResult = CreateAuthClient();

            if (clientResult.IsFailure)
            {
                logger.LogInformation(
                    "GetPermissionsAsync => Acquire API client stopped. Reason={Reason}, NextAction=return failure",
                    clientResult.Message);

                return Result<string[]>.Failure(clientResult.Message, clientResult.ErrorCode);
            }

            using var client = clientResult.Data!;

            logger.LogInformation(
                "GetPermissionsAsync => Client acquired, Reason={Reason}, NextAction=send request",
                clientResult.Message);

            #endregion

            #region Send API request

            var responseResult = await SendPermissionsRequestAsync(client, cancellationToken);

            if (responseResult.IsFailure)
            {
                logger.LogInformation(
                    "GetPermissionsAsync => Send API request stopped. Reason={Reason}, NextAction=return failure",
                    responseResult.Message);

                return Result<string[]>.Failure(responseResult.Message, responseResult.ErrorCode);
            }

            using var response = responseResult.Data!;

            logger.LogInformation(
                "GetPermissionsAsync => API response received, StatusCode={StatusCode}, Reason={Reason}, NextAction=check response status",
                (int)response.StatusCode,
                responseResult.Message);

            #endregion

            #region Check API response

            var statusResult = CheckApiResponse(response);

            if (statusResult.IsFailure)
            {
                logger.LogInformation(
                    "GetPermissionsAsync => Check API response stopped. Reason={Reason}, NextAction=return unauthorized failure",
                    statusResult.Message);

                return Result<string[]>.Failure(statusResult.Message, statusResult.ErrorCode);
            }

            logger.LogInformation(
                "GetPermissionsAsync => Response status accepted, Reason={Reason}, NextAction=read typed payload",
                statusResult.Message);

            #endregion

            #region Read API payload

            var payloadResult = await ReadApiPayloadAsync<string[]>(
                response,
                "The API returned an empty permissions response.",
                cancellationToken);

            if (payloadResult.IsFailure)
            {
                logger.LogInformation(
                    "GetPermissionsAsync => Read API payload stopped. Reason={Reason}, NextAction=return failure",
                    payloadResult.Message);

                return Result<string[]>.Failure(payloadResult.Message, payloadResult.ErrorCode);
            }

            logger.LogInformation(
                "GetPermissionsAsync => Payload read, Reason={Reason}, NextAction=return payload",
                payloadResult.Message);

            #endregion

            return payloadResult;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetPermissionsAsync => Authentication API operation failed.");

            throw;
        }
    }
}
