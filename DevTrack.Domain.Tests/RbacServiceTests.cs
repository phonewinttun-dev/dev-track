using Microsoft.Extensions.Logging.Abstractions;
using DevTrack.Database.Entities;
using DevTrack.Domain.Security;
using DevTrack.Shared.Security;

namespace DevTrack.Domain.Tests;

public class RbacServiceTests
{
    [Fact]
    public async Task SeedAsync_PreservesAdminLoginAndDoesNotDuplicateRecords()
    {
        using var db = TestDbContextFactory.Create();
        var service = new RbacService(db, NullLogger<RbacService>.Instance);
        await service.SeedAsync("admin", "test-password", "Admin");
        await service.SeedAsync("other", "other-password", "Other");

        var user = await service.LoginAsync("admin", "test-password");
        Assert.NotNull(user);
        Assert.Single(db.Users);
        Assert.Single(db.Roles);
        Assert.Equal(Permissions.All.Length, db.Permissions.Count());
        Assert.Equal(Permissions.All.Length, db.RolePermissions.Count());
        var permissions = await service.GetPermissionsAsync(user.Id);
        Assert.Equal(Permissions.All.Order(), permissions.Order());
        Assert.True(await service.HasPermissionAsync(user.Id, Permissions.All[0]));
        Assert.False(await service.HasPermissionAsync(user.Id, "unknown"));
        Assert.Null(await service.LoginAsync("admin", "wrong-password"));

        user.IsActive = false;
        await db.SaveChangesAsync();
        Assert.Null(await service.LoginAsync("admin", "test-password"));
        Assert.Empty(await service.GetPermissionsAsync(user.Id));
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("not-base64:not-base64")]
    public async Task LoginAsync_RejectsMalformedStoredHashes(string storedHash)
    {
        using var db = TestDbContextFactory.Create();
        db.Roles.Add(new AppRole { Id = 1, RoleName = "Admin" });
        db.Users.Add(new AppUser
        {
            Username = "admin", PasswordHash = storedHash, RoleId = 1
        });
        await db.SaveChangesAsync();

        Assert.Null(await new RbacService(db, NullLogger<RbacService>.Instance).LoginAsync("admin", "test-password"));
    }
}
