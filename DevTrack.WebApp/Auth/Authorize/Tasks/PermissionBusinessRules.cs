using Microsoft.AspNetCore.Mvc.Filters;

namespace DevTrack.WebApp.Auth;

public partial class PermissionFilter
{
    private static string[] ReadRequiredPermissions(AuthorizationFilterContext context)
    {
        #region Select the endpoint requirements

        return context.ActionDescriptor.EndpointMetadata
            .OfType<PermissionAttribute>()
            .Select(attribute => attribute.Permission)
            .ToArray();

        #endregion
    }
}
