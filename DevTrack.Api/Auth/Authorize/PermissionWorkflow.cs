using DevTrack.Features.Access.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevTrack.Api.Auth;

public partial class PermissionFilter(
    RbacService service,
    ILogger<PermissionFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            #region Check the authenticated identity

            var authenticationResult = CheckAuthentication(context.HttpContext.User);

            if (authenticationResult.IsFailure)
            {
                logger.LogInformation(
                    "OnAuthorizationAsync => Authentication required. Reason={Reason}, NextAction=challenge",
                    authenticationResult.Message);

                context.Result = new ChallengeResult();

                return;
            }

            logger.LogInformation(
                "OnAuthorizationAsync => Identity authenticated. Reason={Reason}, NextAction=read permission metadata",
                authenticationResult.Message);

            #endregion

            #region Read the required permissions

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
                "OnAuthorizationAsync => Permission metadata selected. Reason={Reason}, NextAction=read user ID",
                metadataResult.Message);

            #endregion

            #region Read the user identifier

            var userIdResult = ReadUserId(context.HttpContext.User);

            if (userIdResult.IsFailure)
            {
                logger.LogInformation(
                    "OnAuthorizationAsync => User ID rejected. Reason={Reason}, NextAction=forbid",
                    userIdResult.Message);

                context.Result = new ForbidResult();

                return;
            }

            var userId = userIdResult.Data;

            logger.LogInformation(
                "OnAuthorizationAsync => User ID read. UserId={UserId}, Reason={Reason}, NextAction=check required permissions",
                userId,
                userIdResult.Message);

            #endregion

            #region Check every required permission in order

            var checkedCount = 0;

            foreach (var permission in requiredPermissions)
            {
                var permissionResult = await CheckPermissionAsync(userId, permission);

                if (permissionResult.IsFailure)
                {
                    logger.LogInformation(
                        "OnAuthorizationAsync => Permission denied. UserId={UserId}, Permission={Permission}, Reason={Reason}, NextAction=forbid",
                        userId,
                        permission,
                        permissionResult.Message);

                    context.Result = new ForbidResult();

                    return;
                }

                checkedCount++;

                logger.LogInformation(
                    "OnAuthorizationAsync => Permission granted. UserId={UserId}, Permission={Permission}, Reason={Reason}, NextAction=check remaining permissions",
                    userId,
                    permission,
                    permissionResult.Message);
            }

            logger.LogInformation(
                "OnAuthorizationAsync => Required permissions checked. UserId={UserId}, CheckedCount={CheckedCount}, NextAction=continue request",
                userId,
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
