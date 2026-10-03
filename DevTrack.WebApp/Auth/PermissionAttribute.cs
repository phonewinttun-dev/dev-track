using System.Security.Claims;
using DevTrack.Domain.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace DevTrack.WebApp.Auth;

public class PermissionAttribute(string permission) : TypeFilterAttribute(typeof(PermissionFilter)) { public string Permission { get; } = permission; }
public class PermissionFilter(RbacService service) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true) { context.Result = new ChallengeResult(); return; }
        var required = context.ActionDescriptor.EndpointMetadata.OfType<PermissionAttribute>().Select(x => x.Permission);
        if (!int.TryParse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) { context.Result = new ForbidResult(); return; }
        foreach (var permission in required) if (!await service.HasPermissionAsync(id, permission)) { context.Result = new ForbidResult(); return; }
    }
}
