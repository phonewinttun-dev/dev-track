using System.Security.Cryptography;
using DevTrack.Database.Entities;
using DevTrack.Shared.Security;
using Microsoft.EntityFrameworkCore;
namespace DevTrack.Domain.Security;

public class RbacService(DevTrackDbContext db)
{
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32));
    }
    public async Task<AppUser?> LoginAsync(string username, string password)
    {
        var user = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Username == username && x.IsActive);
        if (user is null) return null;
        var parts = user.PasswordHash.Split(':');
        if (parts.Length != 2) return null;
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(parts[1]), Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(parts[0]), 100000, HashAlgorithmName.SHA256, 32)) ? user : null; }
        catch (FormatException) { return null; }
    }
    public Task<string[]> GetPermissionsAsync(int userId) => (from u in db.Users from rp in db.RolePermissions where u.Id == userId && u.IsActive && rp.RoleId == u.RoleId select rp.Permission.PermissionName).ToArrayAsync();
    public async Task<bool> HasPermissionAsync(int userId, string permission) => await (from u in db.Users from rp in db.RolePermissions where u.Id == userId && u.IsActive && rp.RoleId == u.RoleId && rp.Permission.PermissionName == permission select u.Id).AnyAsync();
    public async Task SeedAsync(string? username, string? password, string displayName)
    {
        foreach (var name in Permissions.All) if (!await db.Permissions.AnyAsync(x => x.PermissionName == name)) db.Permissions.Add(new() { PermissionName = name });
        await db.SaveChangesAsync();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || await db.Users.AnyAsync()) return;
        var role = await db.Roles.SingleOrDefaultAsync(x => x.RoleName == "Admin");
        if (role is null) { role = new() { RoleName = "Admin" }; db.Roles.Add(role); await db.SaveChangesAsync(); }
        foreach (var permission in await db.Permissions.ToListAsync()) if (!await db.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permission.Id)) db.RolePermissions.Add(new() { RoleId = role.Id, PermissionId = permission.Id });
        db.Users.Add(new() { Username = username, DisplayName = displayName, PasswordHash = HashPassword(password), RoleId = role.Id });
        await db.SaveChangesAsync();
    }
}
