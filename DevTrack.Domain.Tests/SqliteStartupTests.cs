using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using DevTrack.Api;
using DevTrack.Database.Entities;
using DevTrack.Features.Access.Auth;
using DevTrack.Shared.Security;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevTrack.Domain.Tests;

public class SqliteStartupTests
{
    [Fact]
    public async Task Startup_CreatesSqliteAccountsAndPreservesChangesAcrossRestarts()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"devtrack-test-{Guid.NewGuid():N}.db");

        try
        {
            string adminToken;
            await using (var factory = CreateFactory(databasePath))
            {
                using var client = factory.CreateClient();
                adminToken = await LoginAsync(client, "admin", "admin@123");
                var staffToken = await LoginAsync(client, "staff", "staff@123");
                Assert.True(File.Exists(databasePath));
                Assert.Equal(32, (await File.ReadAllBytesAsync(databasePath + ".key")).Length);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
                var staffProfile = await client.GetFromJsonAsync<MeResponse>("/me?userId=1");
                Assert.Equal("staff", staffProfile!.Username);
                Assert.Equal("Staff", staffProfile.Role);
                Assert.Contains(Permissions.BatchesRead, staffProfile.Permissions);
                Assert.DoesNotContain(Permissions.BatchesWrite, staffProfile.Permissions);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/batches")).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden,
                    (await client.PostAsJsonAsync("/api/batches/1/assignments", new[] { 1 })).StatusCode);

                using var scope = factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
                Assert.True(db.Database.IsSqlite());
                Assert.Equal(2, await db.Users.CountAsync());
                Assert.Equal(2, await db.Roles.CountAsync());
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(adminToken)));
                Assert.True(await db.AccessSessions.AnyAsync(session => session.TokenHash == hash));
                var admin = await db.Users.SingleAsync(user => user.Username == "admin");
                Assert.NotEqual("admin@123", admin.PasswordHash);
                admin.PasswordHash = UserAccessService.HashPassword("changed-password");
                var grant = await db.RolePermissions.SingleAsync(grant => grant.RoleId == admin.RoleId
                    && grant.Permission.PermissionName == Permissions.BatchesRead);
                db.RolePermissions.Remove(grant);
                await db.SaveChangesAsync();
            }

            await using (var factory = CreateFactory(databasePath))
            {
                using var client = factory.CreateClient();
                Assert.Equal(HttpStatusCode.Unauthorized,
                    (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin@123"))).StatusCode);
                await LoginAsync(client, "admin", "changed-password");
                await LoginAsync(client, "staff", "staff@123");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
                var adminProfile = await client.GetFromJsonAsync<MeResponse>("/me");
                Assert.Equal("admin", adminProfile!.Username);
                Assert.DoesNotContain(Permissions.BatchesRead, adminProfile.Permissions);
                Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/batches")).StatusCode);
                using var scope = factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
                Assert.Equal(2, await db.Users.CountAsync());
                Assert.Equal(2, await db.Roles.CountAsync());
            }
        }
        finally
        {
            File.Delete(databasePath);
            File.Delete(databasePath + "-wal");
            File.Delete(databasePath + "-shm");
            File.Delete(databasePath + ".key");
        }
    }

    private static WebApplicationFactory<ApiAssemblyMarker> CreateFactory(string databasePath)
    {
        return ApiAuthenticationTests.CreateFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["DatabaseProvider"] = "SQLite",
                    ["AccessToken:SigningKey"] = "",
                    ["AccessToken:KeyFile"] = databasePath + ".key",
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath};Pooling=False"
                })));
    }

    private static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
    }
}
