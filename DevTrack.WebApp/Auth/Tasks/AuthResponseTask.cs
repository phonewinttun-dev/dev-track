using System.Net;
using System.Text.Json;
using DevTrack.Shared;
using DevTrack.Shared.Security;

namespace DevTrack.WebApp.Auth;

public partial class AuthApiClient
{
    private static async Task<Result<TResponse>> ReadAuthResponseAsync<TResponse>(
        HttpResponseMessage response,
        string emptyPayloadMessage,
        CancellationToken cancellationToken) where TResponse : class
    {
        #region Validate status before reading the payload

        var statusResult = CheckApiResponse(response);

        if (statusResult.IsFailure)
        {
            return Result<TResponse>.Failure(statusResult.Message, statusResult.ErrorCode);
        }

        return await ReadApiPayloadAsync<TResponse>(response, emptyPayloadMessage, cancellationToken);

        #endregion
    }

    private static Result CheckApiResponse(HttpResponseMessage response)
    {
        #region Preserve existing HTTP outcomes

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Result.Failure("The API requires authentication.", AuthErrorCodes.Unauthorized);
        }

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure("The authentication API request failed. Please try again.", AuthErrorCodes.ApiFailure);
        }

        return Result.Success("API response status accepted.");

        #endregion
    }

    private static async Task<Result<TResponse>> ReadApiPayloadAsync<TResponse>(
        HttpResponseMessage response,
        string emptyPayloadMessage,
        CancellationToken cancellationToken) where TResponse : class
    {
        #region Read the existing typed response

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);

            if (payload is null)
            {
                return Result<TResponse>.Failure(emptyPayloadMessage, AuthErrorCodes.InvalidResponse);
            }

            return Result<TResponse>.Success(payload, "API response payload read.");
        }
        catch (JsonException)
        {
            return Result<TResponse>.Failure(
                "The authentication API returned an invalid response.",
                AuthErrorCodes.InvalidResponse);
        }

        #endregion
    }
}
