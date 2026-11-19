using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevTrack.WebApp.Auth;

public class PermissionAttribute(string permission) : TypeFilterAttribute(typeof(PermissionFilter))
{
    public string Permission { get; } = permission;
}

public class PermissionFilter(AuthApiClient authApi) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var requiredPermissions = context.ActionDescriptor.EndpointMetadata
            .OfType<PermissionAttribute>()
            .Select(attribute => attribute.Permission);
        var permissions = await authApi.GetPermissionsAsync(context.HttpContext.RequestAborted);
        if (permissions is null)
        {
            await context.HttpContext.SignOutAsync();
            context.Result = new ChallengeResult();
            return;
        }

        if (requiredPermissions.Any(permission => !permissions.Contains(permission, StringComparer.OrdinalIgnoreCase)))
        {
            context.Result = new ForbidResult();
        }
    }
}
