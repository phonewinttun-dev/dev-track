using System.Net.Http.Json;
using DevTrack.Database.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using DevTrack.Api;
using DevTrack.Shared.Security;
using DevTrack.Features.Access.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DevTrack.Domain.Tests;

public class ApiAuthenticationTests
{
    [Theory]
    [InlineData("/api/batches")]
    [InlineData("/me")]
    public async Task ProtectedEndpoint_ReturnsUnauthorized_WithoutToken(string path)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(path);

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

    [Fact]
    public async Task Login_ReturnsOnlyJwtWithRandomSessionClaim()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(AuthTestSettings.Username, AuthTestSettings.Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var property = Assert.Single(payload.RootElement.EnumerateObject());
        Assert.Equal("accessToken", property.Name);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(property.Value.GetString());
        var claim = Assert.Single(jwt.Payload);
        Assert.Equal("sid", claim.Key);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", Assert.IsType<string>(claim.Value));
    }

    [Fact]
    public async Task Logout_RevokesTokenAndRejectsReplay()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/permission-check", new PermissionCheckRequest([]))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/batches")).StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_IsRejectedByAuthentication()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        var session = await db.AccessSessions.SingleAsync();
        session.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/permission-check", new PermissionCheckRequest([]))).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PermissionCheck_ReturnsOnlyDecisionForCurrentGrants(bool hasPermission)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));
        var requestedPermission = hasPermission ? Permissions.BatchesRead : "unknown.permission";

        var response = await client.PostAsJsonAsync("/api/auth/permission-check",
            new PermissionCheckRequest([requestedPermission]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var property = Assert.Single(payload.RootElement.EnumerateObject());
        Assert.Equal("isAllowed", property.Name);
        Assert.Equal(hasPermission, property.Value.GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/auth/permissions")).StatusCode);
    }

    [Fact]
    public async Task PermissionCheck_RequiresAnActiveSessionEvenWithoutRequirements()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/permission-check",
            new PermissionCheckRequest([]))).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));
        var response = await client.PostAsJsonAsync("/api/auth/permission-check", new PermissionCheckRequest([]));
        Assert.True((await response.Content.ReadFromJsonAsync<PermissionCheckResponse>())!.IsAllowed);
    }

    [Fact]
    public async Task PermissionCheck_EvaluatesEveryRequirementAndCurrentGrants()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        var grantedResponse = await client.PostAsJsonAsync("/api/auth/permission-check",
            new PermissionCheckRequest([Permissions.BatchesRead.ToUpperInvariant()]));
        Assert.True((await grantedResponse.Content.ReadFromJsonAsync<PermissionCheckResponse>())!.IsAllowed);
        var mixedResponse = await client.PostAsJsonAsync("/api/auth/permission-check",
            new PermissionCheckRequest([Permissions.BatchesRead, "unknown.permission"]));
        Assert.False((await mixedResponse.Content.ReadFromJsonAsync<PermissionCheckResponse>())!.IsAllowed);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        var grants = await db.RolePermissions.Where(grant => grant.Permission.PermissionName == Permissions.BatchesRead)
            .ToListAsync();
        db.RolePermissions.RemoveRange(grants);
        await db.SaveChangesAsync();

        var revokedResponse = await client.PostAsJsonAsync("/api/auth/permission-check",
            new PermissionCheckRequest([Permissions.BatchesRead]));
        Assert.False((await revokedResponse.Content.ReadFromJsonAsync<PermissionCheckResponse>())!.IsAllowed);
    }

    [Fact]
    public async Task Me_ReturnsCurrentUserDetailsWithoutSecrets()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        using var response = await client.GetAsync("/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var fields = payload.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray();
        Assert.Equal(new[] { "displayName", "permissions", "role", "userId", "username" }, fields);
        var profile = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(profile);
        Assert.True(profile.UserId > 0);
        Assert.Equal("admin", profile.Username);
        Assert.Equal("Admin", profile.DisplayName);
        Assert.Equal("Admin", profile.Role);
        Assert.Equal(Permissions.All.Order(), profile.Permissions.Order());
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Fact]
    public async Task Me_ReflectsProfileRoleAndGrantChangesWithTheSameToken()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        var user = await db.Users.SingleAsync(user => user.Username == "admin");
        var staffRole = await db.Roles.SingleAsync(role => role.RoleName == "Staff");
        user.Username = "renamed-user";
        user.DisplayName = "Updated Name";
        user.RoleId = staffRole.Id;
        await db.SaveChangesAsync();

        var profile = await client.GetFromJsonAsync<MeResponse>("/me");
        Assert.Equal("renamed-user", profile!.Username);
        Assert.Equal("Updated Name", profile.DisplayName);
        Assert.Equal("Staff", profile.Role);
        Assert.Contains(Permissions.BatchesRead, profile.Permissions);
        Assert.DoesNotContain(Permissions.BatchesWrite, profile.Permissions);
        db.RolePermissions.RemoveRange(await db.RolePermissions.Where(grant => grant.RoleId == staffRole.Id).ToListAsync());
        await db.SaveChangesAsync();
        profile = await client.GetFromJsonAsync<MeResponse>("/me");
        Assert.Empty(profile!.Permissions);

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("sid", Assert.Single(jwt.Payload).Key);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("disabled")]
    public async Task Me_RejectsInvalidSessions(string condition)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var token = await LoginAsync(client);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        var session = await db.AccessSessions.SingleAsync();

        switch (condition)
        {
            case "invalid":
                token = "invalid-token";
                break;
            case "expired":
                session.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
                break;
            case "revoked":
                session.IsRevoked = true;
                break;
            case "disabled":
                var user = await db.Users.SingleAsync(user => user.Id == session.UserId);
                user.IsActive = false;
                break;
        }

        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/me")).StatusCode);
    }

    [Fact]
    public async Task Health_ReturnsPublicStatusWithoutUserOrDatabaseDetails()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var property = Assert.Single(payload.RootElement.EnumerateObject());
        Assert.Equal("status", property.Name);
        Assert.Equal("healthy", property.Value.GetString());
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    internal static WebApplicationFactory<ApiAssemblyMarker> CreateFactory()
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
    public async Task ExistingToken_LosesAccess_WhenRolePermissionIsRevoked()
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
    public async Task NonAdminRole_CanReadButCannotWrite_WithExistingToken()
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
    public async Task DisabledUser_CannotLoginOrUseExistingToken()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        var user = await db.Users.SingleAsync(x => x.Username == AuthTestSettings.Username);
        user.IsActive = false;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/batches")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(AuthTestSettings.Username, AuthTestSettings.Password))).StatusCode);
    }
}
