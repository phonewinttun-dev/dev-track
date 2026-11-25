using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using DevTrack.Database.Entities;
using DevTrack.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;
using DevTrack.WebApp;
using DevTrack.WebApp.Services;
using DevTrack.Features.TrainingManagement.Dashboard;
using DevTrack.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevTrack.Domain.Tests;

public class WebAppAuthenticationTests
{
    [Fact]
    public async Task Dashboard_ShowsFailureReasonAndEncodesItsContent()
    {
        #region Open the dashboard with a failed data result

        await using var api = ApiAuthenticationTests.CreateFactory();
        await using var factory = CreateFactory(api).WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddScoped<IDashboardService, FailingDashboardService>()));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        #endregion

        #region Verify the rendered failure outcome

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Unable to load dashboard totals.", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>alert", html);

        #endregion
    }

    [Fact]
    public async Task CreateBatch_ShowsSavedIdAfterRedirect()
    {
        #region Submit a batch through the authenticated form

        await using var api = ApiAuthenticationTests.CreateFactory();
        await using var factory = CreateFactory(api);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);
        var form = await client.GetStringAsync("/Batches/Create");
        var token = ExtractRequestVerificationToken(form);

        var response = await client.PostAsync("/Batches/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["BatchName"] = "Workflow feedback batch",
            ["StartDate"] = "2026-05-01",
            ["EndDate"] = "2026-05-01",
            ["SelectedDays"] = "Friday",
            ["__RequestVerificationToken"] = token
        }));

        #endregion

        #region Verify the redirect renders persisted feedback

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var page = await client.GetAsync(response.Headers.Location);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Batch created successfully. Batch ID:", html);
        Assert.Contains("Workflow feedback batch", html);

        #endregion
    }

    [Fact]
    public async Task Home_RedirectsToLogin_WhenUnauthenticated()
    {
        await using var api = ApiAuthenticationTests.CreateFactory();
        await using var factory = CreateFactory(api);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("/Account/Login?ReturnUrl=%2F", response.Headers.Location?.OriginalString);
        Assert.Contains("ReturnUrl=%2F", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Login_RedirectsToReturnUrl_WhenCredentialsAreValid()
    {
        await using var api = ApiAuthenticationTests.CreateFactory();
        await using var factory = CreateFactory(api);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login?returnUrl=%2FBatches");
        var token = ExtractRequestVerificationToken(await loginPage.Content.ReadAsStringAsync());

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = AuthTestSettings.Username,
            ["Password"] = AuthTestSettings.Password,
            ["ReturnUrl"] = "/Batches",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Batches", response.Headers.Location?.OriginalString);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, cookie => cookie.Contains(".AspNetCore.Cookies"));
        var batches = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, batches.StatusCode);
        Assert.Contains(".NET Full Stack", await batches.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("other-admin", AuthTestSettings.Password)]
    [InlineData(AuthTestSettings.Username, "wrong-password")]
    public async Task Login_ShowsInvalidCredentials_WhenApiRejectsCredentials(string username, string password)
    {
        await using var api = ApiAuthenticationTests.CreateFactory();
        await using var factory = CreateFactory(api);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login");
        var token = ExtractRequestVerificationToken(await loginPage.Content.ReadAsStringAsync());

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = username,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid username or password.", await response.Content.ReadAsStringAsync());
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var cookies) &&
                     cookies.Any(cookie => cookie.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task LoginCookie_ContainsOnlyAnonymousSessionClaimAndProtectedToken()
    {
        await using var api = ApiAuthenticationTests.CreateFactory();
        await using var factory = CreateFactory(api);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetStringAsync("/Account/Login");
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = AuthTestSettings.Username,
            ["Password"] = AuthTestSettings.Password,
            ["__RequestVerificationToken"] = ExtractRequestVerificationToken(loginPage)
        }));

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal));
        var protectedTicket = cookie.Split(';')[0].Split('=', 2)[1];
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = options.TicketDataFormat.Unprotect(protectedTicket);

        Assert.NotNull(ticket);
        var claim = Assert.Single(ticket.Principal.Claims);
        Assert.Equal("sub", claim.Type);
        Assert.Matches("^[A-F0-9]{64}$", claim.Value);
        Assert.Null(ticket.Principal.Identity!.Name);
        Assert.False(ticket.Principal.IsInRole("Admin"));
        var accessToken = ticket.Properties.GetTokenValue("access_token");
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Equal("sid", Assert.Single(jwt.Payload).Key);
        Assert.DoesNotContain(accessToken!, cookie);
        Assert.DoesNotContain(AuthTestSettings.DisplayName, cookie);
    }

    [Fact]
    public async Task Logout_ClearsAuthenticationCookie()
    {
        await using var api = ApiAuthenticationTests.CreateFactory();
        await using var factory = CreateFactory(api);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);
        var deniedPage = await client.GetAsync("/Account/Denied");
        var token = ExtractRequestVerificationToken(await deniedPage.Content.ReadAsStringAsync());

        var response = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.OriginalString);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, cookie => cookie.Contains(".AspNetCore.Cookies") && cookie.Contains("expires=Thu, 01 Jan 1970"));
    }

    [Fact]
    public async Task ExistingCookie_IsDenied_WhenPermissionIsRevoked()
    {
        await using var api = ApiAuthenticationTests.CreateFactory();
        await using var factory = CreateFactory(api);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Developers/Create")).StatusCode);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        db.RolePermissions.RemoveRange(await db.RolePermissions.Where(x => x.Permission.PermissionName == "developers.write").ToListAsync());
        await db.SaveChangesAsync();
        var response = await client.GetAsync("/Developers/Create");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Denied", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AttendancePost_RejectsAnAuthenticatedRequestWithoutAntiforgeryToken()
    {
        await using var api = ApiAuthenticationTests.CreateFactory();
        await using var factory = CreateFactory(api);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);

        var response = await client.PostAsync("/Attendance/Mark", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["BatchId"] = "1" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pages_LoadCurrentUserOnceAndRefreshDisplayAndPermissionsWithoutChangingTheCookie()
    {
        await using var api = ApiAuthenticationTests.CreateFactory();
        var meCalls = 0;
        var permissionCheckCalls = 0;
        await using var factory = CreateFactory(api).WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddHttpClient(ApiClientBase.ClientName)
                .AddHttpMessageHandler(() => new RequestCounter(request =>
                {
                    if (request.RequestUri!.AbsolutePath == "/me")
                    {
                        Interlocked.Increment(ref meCalls);
                    }
                    if (request.RequestUri.AbsolutePath == "/api/auth/permission-check")
                    {
                        Interlocked.Increment(ref permissionCheckCalls);
                    }
                }))));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);
        Assert.Equal(0, meCalls);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        var user = await db.Users.SingleAsync(user => user.Username == "admin");
        var staffRole = await db.Roles.SingleAsync(role => role.RoleName == "Staff");
        user.DisplayName = "<script>current name</script>";
        user.RoleId = staffRole.Id;
        var developerGrant = await db.RolePermissions.SingleAsync(grant => grant.RoleId == staffRole.Id
            && grant.Permission.PermissionName == DevTrack.Shared.Security.Permissions.DevelopersRead);
        db.RolePermissions.Remove(developerGrant);
        await db.SaveChangesAsync();
        var sessionHash = (await db.AccessSessions.SingleAsync()).TokenHash;

        var firstPage = await client.GetStringAsync("/");

        Assert.Equal(1, meCalls);
        Assert.Equal(0, permissionCheckCalls);
        Assert.Contains("&lt;script&gt;current name&lt;/script&gt;", firstPage);
        Assert.DoesNotContain("<script>current name</script>", firstPage);
        Assert.Contains(">Staff</span>", firstPage);
        Assert.DoesNotContain("href=\"/Batches/Create\"", firstPage);
        Assert.DoesNotContain("href=\"/Developers\"", firstPage);
        Assert.Contains("href=\"/Attendance\"", firstPage);

        user.DisplayName = "Updated Current Name";
        var writePermission = await db.Permissions.SingleAsync(permission =>
            permission.PermissionName == DevTrack.Shared.Security.Permissions.BatchesWrite);
        db.RolePermissions.Add(new AppRolePermission { RoleId = staffRole.Id, PermissionId = writePermission.Id });
        await db.SaveChangesAsync();

        var secondPage = await client.GetStringAsync("/");

        Assert.Equal(2, meCalls);
        Assert.Equal(0, permissionCheckCalls);
        Assert.Contains("Updated Current Name", secondPage);
        Assert.Contains("href=\"/Batches/Create\"", secondPage);
        Assert.Equal(sessionHash, (await db.AccessSessions.SingleAsync()).TokenHash);
    }

    [Fact]
    public async Task DisabledUser_IsSignedOutBeforeRenderingProtectedPage()
    {
        await using var api = ApiAuthenticationTests.CreateFactory();
        await using var factory = CreateFactory(api);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
        (await db.Users.SingleAsync(user => user.Username == "admin")).IsActive = false;
        await db.SaveChangesAsync();

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value =>
            value.StartsWith(".AspNetCore.Cookies=;", StringComparison.Ordinal));
    }

    private sealed class RequestCounter(Action<HttpRequestMessage> record) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            record(request);
            return await base.SendAsync(request, cancellationToken);
        }
    }

    private static WebApplicationFactory<WebAppAssemblyMarker> CreateFactory(WebApplicationFactory<ApiAssemblyMarker> api)
        => new WebApplicationFactory<WebAppAssemblyMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services => services.AddHttpClient(ApiClientBase.ClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => api.Server.CreateHandler()));
            });

    private static async Task LoginAsync(HttpClient client)
    {
        var loginPage = await client.GetAsync("/Account/Login");
        var token = ExtractRequestVerificationToken(await loginPage.Content.ReadAsStringAsync());

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = AuthTestSettings.Username,
            ["Password"] = AuthTestSettings.Password,
            ["ReturnUrl"] = "/",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static string ExtractRequestVerificationToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.IgnoreCase);
        Assert.True(match.Success, "The antiforgery token was not found in the rendered form.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private sealed class FailingDashboardService : IDashboardService
    {
        #region Provide a failure to the rendered page

        public async Task<Result<DashboardResponse>> GetDashboardDataAsync()
        {
            return await Task.FromResult(Result<DashboardResponse>.Failure(
                "Unable to load dashboard totals. <script>alert('unsafe')</script>"));
        }

        public Task<Result<List<BatchSummaryModel>>> GetDashboardReportAsync()
        {
            throw new InvalidOperationException("Opening the dashboard must not generate a report.");
        }

        #endregion
    }
}
