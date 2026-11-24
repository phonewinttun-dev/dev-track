using DevTrack.Shared;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevTrack.WebApp.Auth;

public partial class PermissionFilter
{
    private static Result<IEnumerable<string>> ReadRequiredPermissions(AuthorizationFilterContext context)
    {
        #region Select existing endpoint metadata

        var permissions = context.ActionDescriptor.EndpointMetadata
            .OfType<PermissionAttribute>()
            .Select(attribute => attribute.Permission);

        return Result<IEnumerable<string>>.Success(permissions, "Endpoint requirements selected.");

        #endregion
    }

    private async Task<Result<string[]>> LoadUserPermissionsAsync(CancellationToken cancellationToken)
    {
        #region Read the current API permissions

        var permissionsResult = await authApi.GetPermissionsAsync(cancellationToken);

        return permissionsResult;

        #endregion
    }

    private static Result CheckRequiredPermission(string requiredPermission, string[] permissions)
    {
        #region Preserve ordinal case-insensitive permission matching

        if (!permissions.Contains(requiredPermission, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure("The required permission is not granted.");
        }

        return Result.Success("The required permission is granted.");

        #endregion
    }
}
