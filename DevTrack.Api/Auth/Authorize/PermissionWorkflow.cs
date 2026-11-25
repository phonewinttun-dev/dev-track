using DevTrack.Features.Access.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevTrack.Api.Auth;

public partial class PermissionFilter(
    UserAccessService service,
    ILogger<PermissionFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            #region Require a valid authenticated identity

            if (CheckAuthentication(context.HttpContext.User).IsFailure)
            {
                context.Result = new ChallengeResult();
                return;
            }

            var userIdResult = ReadUserId(context.HttpContext.User);

            if (userIdResult.IsFailure)
            {
                context.Result = new ForbidResult();
                return;
            }

            #endregion

            #region Check every required permission

            foreach (var permission in ReadRequiredPermissions(context))
            {
                var result = await CheckPermissionAsync(userIdResult.Data, permission);

                if (result.IsFailure)
                {
                    context.Result = new ForbidResult();
                    logger.LogInformation("OnAuthorizationAsync => Required permission denied.");
                    return;
                }
            }

            logger.LogInformation("OnAuthorizationAsync => Required permissions granted.");

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "OnAuthorizationAsync => Permission evaluation failed.");
            throw;
        }
    }
}
