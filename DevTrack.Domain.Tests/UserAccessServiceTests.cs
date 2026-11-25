using Microsoft.Extensions.Logging.Abstractions;
using DevTrack.Domain;
using DevTrack.Database.Entities;
using DevTrack.Features.Access.Auth;
using DevTrack.Shared.Security;

namespace DevTrack.Domain.Tests;

public class UserAccessServiceTests
{
    [Fact]
    public async Task InitializeAsync_PreservesAdminLoginAndDoesNotDuplicateRecords()
    {
        using var db = TestDbContextFactory.Create();
        var service = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
        var setup = new DatabaseSetup(db, NullLogger<DatabaseSetup>.Instance);
        await setup.InitializeAsync();
        await setup.InitializeAsync();

        var user = await service.LoginAsync("admin", "admin@123");
        Assert.NotNull(user);
        Assert.Equal(2, db.Users.Count());
        Assert.Equal(2, db.Roles.Count());
        Assert.Equal(Permissions.All.Length, db.Permissions.Count());
        Assert.Equal(Permissions.All.Length + 4, db.RolePermissions.Count());
        var permissions = await service.GetPermissionsAsync(user.Id);
        Assert.Equal(Permissions.All.Order(), permissions.Order());
        Assert.True(await service.HasPermissionAsync(user.Id, Permissions.All[0]));
        Assert.False(await service.HasPermissionAsync(user.Id, "unknown"));
        Assert.Null(await service.LoginAsync("admin", "wrong-password"));

        user.IsActive = false;
        await db.SaveChangesAsync();
        Assert.Null(await service.LoginAsync("admin", "admin@123"));
        Assert.Empty(await service.GetPermissionsAsync(user.Id));
    }

    [Fact]
    public async Task GetMeAsync_ReturnsFailureForMissingUser()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new UserAccessService(db, NullLogger<UserAccessService>.Instance);

        Assert.True((await service.GetMeAsync(1)).IsFailure);
    }

    [Fact]
    public async Task GetMeAsync_PropagatesCancellationAndStoreFailure()
    {
        var db = TestDbContextFactory.Create();
        var service = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetMeAsync(1, cancellation.Token));
        await db.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.GetMeAsync(1));
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
            Username = "admin",
            PasswordHash = storedHash,
            RoleId = 1
        });
        await db.SaveChangesAsync();

        Assert.Null(await new UserAccessService(db, NullLogger<UserAccessService>.Instance).LoginAsync("admin", "test-password"));
    }
}
