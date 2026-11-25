using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using DevTrack.Shared.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevTrack.WebApp.Auth;

public partial class PermissionFilter(
    AuthApiClient authApi,
    CurrentUser currentUser,
    ILogger<PermissionFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            #region Require an authenticated session

            if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            {
                return;
            }

            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            #endregion

            #region Load current user details and permissions

            var result = await authApi.GetMeAsync(context.HttpContext.RequestAborted);

            if (result.IsFailure)
            {
                if (result.ErrorCode == AuthErrorCodes.Unauthorized)
                {
                    await context.HttpContext.SignOutAsync();
                    context.Result = new ChallengeResult();
                    logger.LogInformation("OnAuthorizationAsync => Expired session signed out.");
                }
                else
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    logger.LogError("OnAuthorizationAsync => Permission check failed. Reason={Reason}", result.Message);
                }

                return;
            }

            #endregion

            #region Apply the current permissions

            currentUser.Details = result.Data!;
            var isAllowed = ReadRequiredPermissions(context).All(currentUser.HasPermission);

            if (!isAllowed)
            {
                context.Result = new ForbidResult();
            }

            logger.LogInformation("OnAuthorizationAsync => Permission check completed. IsAllowed={IsAllowed}",
                isAllowed);

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "OnAuthorizationAsync => Permission evaluation failed.");
            throw;
        }
    }
}
