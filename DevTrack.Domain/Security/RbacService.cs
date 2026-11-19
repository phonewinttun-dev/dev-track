using System.Security.Cryptography;
using DevTrack.Database.Entities;
using DevTrack.Shared.Security;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace DevTrack.Domain.Security;

public class RbacService(DevTrackDbContext db, ILogger<RbacService> logger)
{
    private readonly ILogger<RbacService> logger = logger;

    private const int SaltSize = 16;
    private const int PasswordIterations = 100000;
    private const int PasswordKeySize = 32;
    private const string AdminRoleName = "Admin";

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = DerivePasswordHash(password, salt);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public async Task<AppUser?> LoginAsync(string username, string password)
    {
        try
        {
            var user = await db.Users
                .Include(user => user.Role)
                .SingleOrDefaultAsync(user => user.Username == username && user.IsActive);

            if (user is null)
            {
                return null;
            }

            return VerifyPassword(password, user.PasswordHash) ? user : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "LoginAsync => Operation failed.");
            throw;
        }
    }

    public async Task<string[]> GetPermissionsAsync(int userId)
    {
        try
        {
            return await GetUserPermissions(userId).ToArrayAsync();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetPermissionsAsync => Operation failed.");
            throw;
        }
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        try
        {
            return await GetUserPermissions(userId).AnyAsync(name => name == permission);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "HasPermissionAsync => Operation failed.");
            throw;
        }
    }

    public async Task SeedAsync(string? username, string? password, string displayName)
    {
        try
        {
            await using var transaction = db.Database.IsSqlServer()
                ? await db.Database.BeginTransactionAsync()
                : null;
            if (transaction is not null)
            {
                // Both hosts seed the same database. Hold the lock until all bootstrap writes commit.
                await db.Database.ExecuteSqlRawAsync("""
                    DECLARE @result int;
                    EXEC @result = sys.sp_getapplock
                        @Resource = N'DevTrack.RbacSeed', @LockMode = N'Exclusive',
                        @LockOwner = N'Transaction', @LockTimeout = 15000;
                    IF @result < 0
                        THROW 50001, 'Unable to acquire the RBAC bootstrap lock.', 1;
                    """);
            }

            await SeedPermissionsAsync();

            if (!string.IsNullOrWhiteSpace(username) &&
                !string.IsNullOrWhiteSpace(password) &&
                !await db.Users.AnyAsync())
            {
                var role = await GetOrCreateAdminRoleAsync();
                await AssignAdminPermissionsAsync(role.Id);

                db.Users.Add(new AppUser
                {
                    Username = username,
                    DisplayName = displayName,
                    PasswordHash = HashPassword(password),
                    RoleId = role.Id
                });
                await db.SaveChangesAsync();
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "SeedAsync => Operation failed.");
            throw;
        }
    }

    private IQueryable<string> GetUserPermissions(int userId)
    {
        return from user in db.Users
               from rolePermission in db.RolePermissions
               where user.Id == userId && user.IsActive && rolePermission.RoleId == user.RoleId
               select rolePermission.Permission.PermissionName;
    }

    private static byte[] DerivePasswordHash(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password, salt, PasswordIterations, HashAlgorithmName.SHA256, PasswordKeySize);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var expectedHash = Convert.FromBase64String(parts[1]);
            var salt = Convert.FromBase64String(parts[0]);
            var actualHash = DerivePasswordHash(password, salt);
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch (FormatException)
        {
            // Malformed stored hashes cannot authenticate a user.
            return false;
        }
    }

    private async Task SeedPermissionsAsync()
    {
        foreach (var name in Permissions.All)
        {
            if (!await db.Permissions.AnyAsync(permission => permission.PermissionName == name))
            {
                db.Permissions.Add(new AppPermission { PermissionName = name });
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task<AppRole> GetOrCreateAdminRoleAsync()
    {
        var role = await db.Roles.SingleOrDefaultAsync(role => role.RoleName == AdminRoleName);
        if (role is not null)
        {
            return role;
        }

        role = new AppRole { RoleName = AdminRoleName };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    private async Task AssignAdminPermissionsAsync(int roleId)
    {
        foreach (var permission in await db.Permissions.ToListAsync())
        {
            var isAssigned = await db.RolePermissions.AnyAsync(
                assignment => assignment.RoleId == roleId && assignment.PermissionId == permission.Id);
            if (!isAssigned)
            {
                db.RolePermissions.Add(new AppRolePermission
                {
                    RoleId = roleId,
                    PermissionId = permission.Id
                });
            }
        }
    }
}
