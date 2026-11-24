using DevTrack.Shared.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevTrack.WebApp.Auth;

public partial class PermissionFilter(
    AuthApiClient authApi,
    ILogger<PermissionFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            #region Check the authenticated identity

            var identityResult = CheckAuthentication(context.HttpContext.User);

            if (identityResult.IsFailure)
            {
                logger.LogInformation(
                    "OnAuthorizationAsync => Authentication required. Reason={Reason}, NextAction=challenge",
                    identityResult.Message);

                context.Result = new ChallengeResult();

                return;
            }

            logger.LogInformation(
                "OnAuthorizationAsync => Identity authenticated. Reason={Reason}, NextAction=read permission metadata",
                identityResult.Message);

            #endregion

            #region Read endpoint requirements

            var metadataResult = ReadRequiredPermissions(context);

            if (metadataResult.IsFailure)
            {
                logger.LogError(
                    "OnAuthorizationAsync => Metadata lookup failed. Reason={Reason}, NextAction=return server failure",
                    metadataResult.Message);

                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);

                return;
            }

            var requiredPermissions = metadataResult.Data!;

            logger.LogInformation(
                "OnAuthorizationAsync => Metadata selected. Reason={Reason}, NextAction=fetch current API permissions",
                metadataResult.Message);

            #endregion

            #region Fetch current permissions and end an expired session

            var permissionsResult = await LoadUserPermissionsAsync(context.HttpContext.RequestAborted);

            if (permissionsResult.IsFailure)
            {
                if (permissionsResult.ErrorCode != AuthErrorCodes.Unauthorized)
                {
                    logger.LogError(
                        "OnAuthorizationAsync => Permission lookup failed. Reason={Reason}, NextAction=return server failure",
                        permissionsResult.Message);

                    context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);

                    return;
                }

                logger.LogInformation(
                    "OnAuthorizationAsync => API session unauthorized. Reason={Reason}, NextAction=sign out then challenge",
                    permissionsResult.Message);

                var signOutResult = await SignOutSessionAsync(context.HttpContext);

                if (signOutResult.IsFailure)
                {
                    logger.LogError(
                        "OnAuthorizationAsync => Sign out failed. Reason={Reason}, NextAction=return server failure",
                        signOutResult.Message);

                    context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);

                return;
                }

                logger.LogInformation(
                    "OnAuthorizationAsync => Session signed out. Reason={Reason}, NextAction=challenge",
                    signOutResult.Message);

                context.Result = new ChallengeResult();

                return;
            }

            var permissions = permissionsResult.Data!;

            logger.LogInformation(
                "OnAuthorizationAsync => Current permissions loaded. PermissionCount={PermissionCount}, Reason={Reason}, NextAction=check requirements",
                permissions.Length,
                permissionsResult.Message);

            #endregion

            #region Check each requirement with existing case-insensitive matching

            var checkedCount = 0;

            foreach (var permission in requiredPermissions)
            {
                var requirementResult = CheckRequiredPermission(permission, permissions);

                if (requirementResult.IsFailure)
                {
                    logger.LogInformation(
                        "OnAuthorizationAsync => Permission denied. Permission={Permission}, Reason={Reason}, NextAction=forbid",
                        permission,
                        requirementResult.Message);

                    context.Result = new ForbidResult();

                    return;
                }

                checkedCount++;

                logger.LogInformation(
                    "OnAuthorizationAsync => Permission granted. Permission={Permission}, Reason={Reason}, NextAction=check remaining requirements",
                    permission,
                    requirementResult.Message);
            }

            logger.LogInformation(
                "OnAuthorizationAsync => Requirements satisfied. CheckedCount={CheckedCount}, NextAction=continue request",
                checkedCount);

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "OnAuthorizationAsync => Permission evaluation failed.");

            throw;
        }
    }
}
