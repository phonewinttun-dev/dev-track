using DevTrack.Shared;
using Microsoft.Extensions.Logging;

namespace DevTrack.Features.Access.Auth;

public partial class UserAccessService
{
    public async Task<Result<PermissionCheckResponse>> CheckPermissionsAsync(
        int userId, PermissionCheckRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            #region Validate the requested requirements

            if (request.RequiredPermissions is null || request.RequiredPermissions.Any(string.IsNullOrWhiteSpace))
            {
                return Result<PermissionCheckResponse>.Failure("Permission requirements must be non-empty names.");
            }

            #endregion

            #region Resolve the server-side decision

            var decision = await EvaluatePermissionRequirementsAsync(userId, request, cancellationToken);

            logger.LogInformation("CheckPermissionsAsync => Permission decision evaluated.");

            return Result<PermissionCheckResponse>.Success(decision);

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "CheckPermissionsAsync => Permission evaluation failed.");
            throw;
        }
    }
}
