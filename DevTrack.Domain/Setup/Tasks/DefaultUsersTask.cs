using DevTrack.Database.Entities;
using DevTrack.Features.Access.Auth;
using DevTrack.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Domain;

public partial class DatabaseSetup
{
    private async Task AddPermissionsAsync(CancellationToken cancellationToken)
    {
        #region Add missing permission definitions

        var existingNames = await db.Permissions.Select(permission => permission.PermissionName)
            .ToListAsync(cancellationToken);

        foreach (var name in Permissions.All.Except(existingNames))
        {
            db.Permissions.Add(new AppPermission { PermissionName = name });
        }

        await db.SaveChangesAsync(cancellationToken);

        #endregion
    }

    private async Task AddDefaultUsersAsync(CancellationToken cancellationToken)
    {
        #region Create the requested accounts without changing existing users

        await AddUserAsync(new LoginRequest("admin", "admin@123"), "Admin", Permissions.All, cancellationToken);
        string[] staffPermissions =
        [
            Permissions.DashboardRead,
            Permissions.BatchesRead,
            Permissions.DevelopersRead,
            Permissions.TrainingRead
        ];
        await AddUserAsync(new LoginRequest("staff", "staff@123"), "Staff", staffPermissions, cancellationToken);

        #endregion
    }

    private async Task AddUserAsync(LoginRequest account, string roleName,
        string[] permissions, CancellationToken cancellationToken)
    {
        #region Add only a missing account and its initial role

        if (await db.Users.AnyAsync(user => user.Username == account.Username, cancellationToken))
        {
            return;
        }

        var role = await db.Roles.SingleOrDefaultAsync(role => role.RoleName == roleName, cancellationToken);

        if (role is null)
        {
            role = new AppRole { RoleName = roleName };
            db.Roles.Add(role);
            await db.SaveChangesAsync(cancellationToken);
            var grants = await db.Permissions.Where(permission => permissions.Contains(permission.PermissionName))
                .ToListAsync(cancellationToken);

            foreach (var permission in grants)
            {
                db.RolePermissions.Add(new AppRolePermission { RoleId = role.Id, PermissionId = permission.Id });
            }
        }

        db.Users.Add(new AppUser
        {
            Username = account.Username,
            DisplayName = roleName,
            PasswordHash = UserAccessService.HashPassword(account.Password),
            RoleId = role.Id
        });
        await db.SaveChangesAsync(cancellationToken);

        #endregion
    }
}
