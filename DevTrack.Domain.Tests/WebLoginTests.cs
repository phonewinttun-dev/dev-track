using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DevTrack.Database.Entities;
using DevTrack.Features.Access.Auth;
using DevTrack.WebApp;
using DevTrack.WebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DevTrack.Domain.Tests;

public class WebLoginTests
{
    [Theory]
    [InlineData("admin", "admin@123", true)]
    [InlineData("staff", "staff@123", false)]
    public async Task LoginAndLogout_EnforcePermissionsWithOneMeCallAndSidOnlyJwt(
        string username, string password, bool canWrite)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"devtrack-web-login-{Guid.NewGuid():N}.db");

        try
        {
            await using var api = ApiAuthenticationTests.CreateFactory().WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["DatabaseProvider"] = "SQLite",
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath};Pooling=False"
                    })));
            var counter = new RequestCounter(api.Server.CreateHandler());
            await using var web = new WebApplicationFactory<WebAppAssemblyMarker>().WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => services.AddHttpClient(ApiClientBase.ClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => counter)));
            using var client = web.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            #region Sign in through the real MVC form and inspect the protected ticket

            var account = new LoginRequest(username, password);
            var token = await LoginAsync(web, client, account);
            Assert.Equal(0, counter.MeCalls);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var claim = Assert.Single(jwt.Payload);
            Assert.Equal("sid", claim.Key);
            var sessionId = Assert.IsType<string>(claim.Value);
            Assert.Matches("^[A-Za-z0-9_-]{43}$", sessionId);
            Assert.Equal(32, Base64UrlEncoder.DecodeBytes(sessionId).Length);
            Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);

            #endregion

            #region Check read pages, write pages, and an actual write request

            var dashboard = await client.GetStringAsync("/");
            Assert.Equal(1, counter.MeCalls);
            var verificationToken = GetFormToken(dashboard);
            Assert.Equal(canWrite, dashboard.Contains("href=\"/Batches/Create\"", StringComparison.Ordinal));

            foreach (var path in new[] { "/Batches", "/Developers" })
            {
                var previousCalls = counter.MeCalls;
                using var page = await client.GetAsync(path);
                Assert.Equal(HttpStatusCode.OK, page.StatusCode);
                Assert.Equal(previousCalls + 1, counter.MeCalls);
            }

            foreach (var path in new[] { "/Batches/Create", "/Developers/Create" })
            {
                var previousCalls = counter.MeCalls;
                using var page = await client.GetAsync(path);
                Assert.Equal(canWrite ? HttpStatusCode.OK : HttpStatusCode.Redirect, page.StatusCode);
                if (!canWrite)
                {
                    Assert.Contains("/Account/Denied", page.Headers.Location!.OriginalString);
                }
                Assert.Equal(previousCalls + 1, counter.MeCalls);
            }

            using var scope = api.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DevTrackDbContext>();
            var originalBatchCount = await db.Batches.CountAsync();
            var callsBeforeWrite = counter.MeCalls;
            using var write = await client.PostAsync("/Batches/Create", new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["BatchName"] = username + " login test batch",
                    ["StartDate"] = "2026-05-01",
                    ["EndDate"] = "2026-05-01",
                    ["SelectedDays"] = "Friday",
                    ["__RequestVerificationToken"] = verificationToken
                }));
            Assert.Equal(HttpStatusCode.Redirect, write.StatusCode);
            Assert.Contains(canWrite ? "/Batches" : "/Account/Denied", write.Headers.Location!.OriginalString);
            Assert.Equal(originalBatchCount + (canWrite ? 1 : 0), await db.Batches.CountAsync());
            Assert.Equal(callsBeforeWrite + 1, counter.MeCalls);

            #endregion

            #region Sign out, reject replay, and sign in again with a fresh session

            var callsBeforeLogout = counter.MeCalls;
            using var logout = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(
                new Dictionary<string, string> { ["__RequestVerificationToken"] = verificationToken }));
            Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
            Assert.Equal("/Account/Login", logout.Headers.Location!.OriginalString);
            Assert.Equal(callsBeforeLogout + 1, counter.MeCalls);
            Assert.Equal(1, counter.LogoutCalls);
            Assert.Contains(logout.Headers.GetValues("Set-Cookie"), value =>
                value.StartsWith(".AspNetCore.Cookies=;", StringComparison.Ordinal));
            var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
            Assert.True(await db.AccessSessions.AnyAsync(session => session.TokenHash == tokenHash && session.IsRevoked));

            var callsAfterLogout = counter.MeCalls;
            using var signedOutPage = await client.GetAsync("/Batches");
            Assert.Equal(HttpStatusCode.Redirect, signedOutPage.StatusCode);
            Assert.Contains("/Account/Login", signedOutPage.Headers.Location!.OriginalString);
            Assert.Equal(callsAfterLogout, counter.MeCalls);
            using var replayClient = api.CreateClient();
            replayClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var replay = await replayClient.GetAsync("/me");
            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

            var newToken = await LoginAsync(web, client, account);
            Assert.NotEqual(token, newToken);
            var newJwt = new JwtSecurityTokenHandler().ReadJwtToken(newToken);
            Assert.Equal("sid", Assert.Single(newJwt.Payload).Key);
            Assert.NotEqual(sessionId, newJwt.Payload["sid"]);
            using var signedInPage = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, signedInPage.StatusCode);
            Assert.Equal(callsAfterLogout + 1, counter.MeCalls);

            #endregion
        }
        finally
        {
            File.Delete(databasePath);
            File.Delete(databasePath + "-wal");
            File.Delete(databasePath + "-shm");
        }
    }

    private static async Task<string> LoginAsync(
        WebApplicationFactory<WebAppAssemblyMarker> web, HttpClient client, LoginRequest account)
    {
        var form = await client.GetStringAsync("/Account/Login");
        using var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Username"] = account.Username,
                ["Password"] = account.Password,
                ["ReturnUrl"] = "/",
                ["__RequestVerificationToken"] = GetFormToken(form)
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value =>
            value.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal));
        var settings = web.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = settings.TicketDataFormat.Unprotect(cookie.Split(';')[0].Split('=', 2)[1]);
        Assert.NotNull(ticket);
        Assert.Equal("sub", Assert.Single(ticket.Principal.Claims).Type);
        return ticket.Properties.GetTokenValue("access_token")!;
    }

    private static string GetFormToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.IgnoreCase);
        Assert.True(match.Success, "The form must include an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private sealed class RequestCounter(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
    {
        private int meCalls;
        private int logoutCalls;

        public int MeCalls => Volatile.Read(ref meCalls);
        public int LogoutCalls => Volatile.Read(ref logoutCalls);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == "/me")
            {
                Interlocked.Increment(ref meCalls);
            }
            else if (request.RequestUri.AbsolutePath == "/api/auth/logout")
            {
                Interlocked.Increment(ref logoutCalls);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
