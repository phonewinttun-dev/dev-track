using DevTrack.Api.Auth.Authorize.Tasks;
using DevTrack.Shared;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DevTrack.Api.Auth;

public partial class PermissionFilter
{
    private static IEnumerable<string> ReadRequiredPermissions(AuthorizationFilterContext context)
    {
        #region Select the existing endpoint requirements

        var permissions = context.ActionDescriptor.EndpointMetadata
            .OfType<PermissionAttribute>()
            .Select(attribute => attribute.Permission);

        return permissions;

        #endregion
    }

    private async Task<Result> CheckPermissionAsync(int userId, string permission)
    {
        #region Evaluate the current permission grant

        var hasPermission = await service.HasPermissionAsync(userId, permission);

        if (!hasPermission)
        {
            return Result.Failure("The required permission is not granted.");
        }

        return Result.Success("The required permission is granted.");

        #endregion
    }
}
