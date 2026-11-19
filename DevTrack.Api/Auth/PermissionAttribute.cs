using System.Security.Claims;
using DevTrack.Features.Access.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevTrack.Api.Auth;

public class PermissionAttribute(string permission) : TypeFilterAttribute(typeof(PermissionFilter))
{
    public string Permission { get; } = permission;
}

public class PermissionFilter(RbacService service) : IAsyncAuthorizationFilter
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
        if (!int.TryParse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            context.Result = new ForbidResult();
            return;
        }
        foreach (var permission in requiredPermissions)
        {
            if (!await service.HasPermissionAsync(userId, permission))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
