using System.Net.Http.Json;
using DevTrack.Database.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using DevTrack.Api;
using DevTrack.Shared.Security;
using DevTrack.WebApp.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace DevTrack.Domain.Tests;

public class ApiAuthenticationTests
{
    [Fact]
    public async Task BatchesEndpoint_ReturnsUnauthorized_WithoutToken()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/batches");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BatchesEndpoint_ReturnsUnauthorized_WithInvalidToken()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await client.GetAsync("/api/batches");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BatchesEndpoint_ReturnsOk_WithValidAdminToken()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        var response = await client.GetAsync("/api/batches");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplicationFactory<ApiAssemblyMarker> CreateFactory()
        => new WebApplicationFactory<ApiAssemblyMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(AuthTestSettings.ApiConfiguration);
                    config.AddInMemoryCollection(new Dictionary<string, string?> { ["DatabaseName"] = Guid.NewGuid().ToString() });
                });
            });

    private static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthTestSettings.Username, AuthTestSettings.Password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
    }

    [Fact]
    public async Task ExistingJwt_LosesAccess_WhenRolePermissionIsRevoked()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/batches")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        var grants = await db.RolePermissions.Where(x => x.Permission.PermissionName == Permissions.BatchesRead).ToListAsync();
        db.RolePermissions.RemoveRange(grants);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/batches")).StatusCode);
    }

    [Fact]
    public async Task NonAdminRole_CanReadButCannotWrite_WithExistingJwt()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        var role = new AppRole { RoleName = "Reader" };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        var read = await db.Permissions.SingleAsync(x => x.PermissionName == Permissions.BatchesRead);
        db.RolePermissions.Add(new() { RoleId = role.Id, PermissionId = read.Id });
        var user = await db.Users.SingleAsync(x => x.Username == AuthTestSettings.Username);
        user.RoleId = role.Id;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/batches")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/batches/1/assignments", new[] { 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/developers")).StatusCode);
    }

    [Fact]
    public async Task Login_RejectsWrongPassword()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthTestSettings.Username, "wrong-password"))).StatusCode);
    }

    [Fact]
    public async Task DisabledUser_CannotLoginOrUseExistingJwt()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        var user = await db.Users.SingleAsync(x => x.Username == AuthTestSettings.Username);
        user.IsActive = false;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/batches")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthTestSettings.Username, AuthTestSettings.Password))).StatusCode);
    }
}
