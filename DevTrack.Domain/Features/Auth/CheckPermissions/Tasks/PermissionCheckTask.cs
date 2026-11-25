namespace DevTrack.Features.Access.Auth;

public partial class UserAccessService
{
    private async Task<PermissionCheckResponse> EvaluatePermissionRequirementsAsync(
        int userId, PermissionCheckRequest request, CancellationToken cancellationToken)
    {
        #region Match every requirement against current server-side grants

        var grantedPermissions = await GetPermissionsAsync(userId, cancellationToken);
        var isAllowed = request.RequiredPermissions.All(permission =>
            grantedPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase));
        return new PermissionCheckResponse(isAllowed);

        #endregion
    }
}
