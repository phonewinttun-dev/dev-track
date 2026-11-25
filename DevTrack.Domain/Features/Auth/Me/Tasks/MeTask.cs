using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.Access.Auth;

public partial class UserAccessService
{
    private async Task<MeResponse?> FindMeAsync(int userId, CancellationToken cancellationToken)
    {
        #region Select only the current profile fields and current role's grants

        return await db.Users.AsNoTracking()
            .Where(user => user.Id == userId && user.IsActive)
            .Select(user => new MeResponse(
                user.Id,
                user.Username,
                user.DisplayName,
                user.Role.RoleName,
                db.RolePermissions.Where(grant => grant.RoleId == user.RoleId)
                    .OrderBy(grant => grant.Permission.PermissionName)
                    .Select(grant => grant.Permission.PermissionName)
                    .ToArray()))
            .SingleOrDefaultAsync(cancellationToken);

        #endregion
    }
}
