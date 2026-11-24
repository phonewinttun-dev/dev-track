using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevTrack.Api.Auth;
using DevTrack.Api.Auth.Authorize.Tasks;
using DevTrack.Features.Access.Auth;
using DevTrack.Shared.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DevTrack.Domain.Tests;

public class AuthWorkflowTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Cookies")]
    [InlineData("bearer")]
    public void Configure_LeavesOtherSchemesUnchanged(string? scheme)
    {
        #region Configure an unrelated scheme

        var configuration = new ConfigureJwtBearerOptions(
            CreateOptions(), NullLogger<ConfigureJwtBearerOptions>.Instance);
        var options = new JwtBearerOptions();
        var original = options.TokenValidationParameters;

        configuration.Configure(scheme, options);

        #endregion

        #region Verify the existing no-op behavior

        Assert.Same(original, options.TokenValidationParameters);

        #endregion
    }

    [Fact]
    public void Configure_PreservesBearerValidationPolicyAndDefaultOverload()
    {
        #region Configure the bearer policy

        var configuration = new ConfigureJwtBearerOptions(
            CreateOptions(), NullLogger<ConfigureJwtBearerOptions>.Instance);
        var options = new JwtBearerOptions();

        configuration.Configure(JwtBearerDefaults.AuthenticationScheme, options);

        #endregion

        #region Verify validation settings and signing key

        var policy = options.TokenValidationParameters;
        Assert.True(policy.ValidateIssuer);
        Assert.True(policy.ValidateAudience);
        Assert.True(policy.ValidateIssuerSigningKey);
        Assert.True(policy.ValidateLifetime);
        Assert.Equal(AuthTestSettings.Issuer, policy.ValidIssuer);
        Assert.Equal(AuthTestSettings.Audience, policy.ValidAudience);
        Assert.Equal(TimeSpan.FromMinutes(1), policy.ClockSkew);
        var key = Assert.IsType<SymmetricSecurityKey>(policy.IssuerSigningKey);
        Assert.Equal(Encoding.UTF8.GetBytes(AuthTestSettings.SigningKey), key.Key);

        configuration.Configure(options);

        Assert.Same(policy, options.TokenValidationParameters);

        #endregion
    }

    [Fact]
    public void CreateToken_PreservesClaimsAndLifetimeWithoutRepeatedEnumerationOrSensitiveLogs()
    {
        #region Create a token from a counted claims sequence

        var logger = new WorkflowLogger<JwtTokenService>();
        var service = new JwtTokenService(CreateOptions(), logger);
        var visitedClaims = 0;
        Claim[] inputClaims =
        [
            new(ClaimTypes.NameIdentifier, "user-private-value"),
            new("permission", "training.read")
        ];
        var claims = inputClaims.Select(claim =>
        {
            visitedClaims++;

            return claim;
        });

        var tokenResult = service.CreateToken(claims);

        Assert.True(tokenResult.IsSuccess);
        Assert.NotNull(tokenResult.Data);
        var serialized = tokenResult.Data;
        var token = new JwtSecurityTokenHandler().ReadJwtToken(serialized);

        #endregion

        #region Verify token semantics and diagnostic safety

        Assert.Equal(inputClaims.Length, visitedClaims);
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
        Assert.Equal(AuthTestSettings.Issuer, token.Issuer);
        Assert.Equal(AuthTestSettings.Audience, Assert.Single(token.Audiences));
        Assert.Equal(TimeSpan.FromMinutes(25), token.ValidTo - token.ValidFrom);
        Assert.Contains(token.Claims, claim => claim.Value == "user-private-value");
        Assert.Contains(token.Claims, claim => claim.Type == "permission" && claim.Value == "training.read");
        var logs = string.Join("\n", logger.Messages);
        Assert.DoesNotContain(AuthTestSettings.SigningKey, logs);
        Assert.DoesNotContain(serialized, logs);
        Assert.DoesNotContain("user-private-value", logs);
        Assert.Contains("NextAction=return token", logs);

        #endregion
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateToken_PropagatesOriginalFailureAndDoesNotContinue(bool isCancellation)
    {
        #region Fail while the token consumes its claims

        var logger = new WorkflowLogger<JwtTokenService>();
        var service = new JwtTokenService(CreateOptions(), logger);
        Exception failure = isCancellation
            ? new OperationCanceledException()
            : new InvalidOperationException("Claims enumeration failed.");
        var claims = new[] { 1 }.Select<int, Claim>(_ => throw failure);

        var thrown = Record.Exception(() => service.CreateToken(claims));

        #endregion

        #region Verify exception identity and skipped serialization

        Assert.Same(failure, thrown);
        Assert.Equal(isCancellation ? null : failure, logger.Exception);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("Token serialized."));

        #endregion
    }

    [Theory]
    [InlineData(false, "1", true, typeof(ChallengeResult))]
    [InlineData(true, null, true, typeof(ForbidResult))]
    [InlineData(true, "invalid", true, typeof(ForbidResult))]
    [InlineData(true, "1", false, null)]
    public async Task OnAuthorizationAsync_StopsBeforeUnnecessaryPermissionLookups(
        bool isAuthenticated,
        string? userId,
        bool requiresPermission,
        Type? expectedResult)
    {
        #region Make any unexpected database lookup fail

        var db = TestDbContextFactory.Create();
        await db.DisposeAsync();
        var logger = new WorkflowLogger<PermissionFilter>();
        var service = new RbacService(db, NullLogger<RbacService>.Instance);
        var filter = new PermissionFilter(service, logger);
        var context = CreateAuthorizationContext(isAuthenticated, userId, requiresPermission);

        await filter.OnAuthorizationAsync(context);

        #endregion

        #region Verify challenge, forbid or pass-through without a query

        Assert.Equal(expectedResult, context.Result?.GetType());
        Assert.Null(logger.Exception);

        #endregion
    }

    [Fact]
    public async Task OnAuthorizationAsync_LogsAndRethrowsPermissionLookupFailure()
    {
        #region Fail an actual required permission lookup

        var db = TestDbContextFactory.Create();
        await db.DisposeAsync();
        var logger = new WorkflowLogger<PermissionFilter>();
        var filter = new PermissionFilter(new RbacService(db, NullLogger<RbacService>.Instance), logger);
        var context = CreateAuthorizationContext(true, "1", true);

        var thrown = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => filter.OnAuthorizationAsync(context));

        #endregion

        #region Verify the failure is not represented as a denial

        Assert.Same(thrown, logger.Exception);
        Assert.Null(context.Result);
        Assert.StartsWith("OnAuthorizationAsync =>", logger.Messages[^1]);

        #endregion
    }

    private static IOptions<JwtOptions> CreateOptions()
    {
        #region Provide isolated signing configuration

        return Options.Create(new JwtOptions
        {
            Issuer = AuthTestSettings.Issuer,
            Audience = AuthTestSettings.Audience,
            SigningKey = AuthTestSettings.SigningKey,
            ExpiresMinutes = 25
        });

        #endregion
    }

    private static AuthorizationFilterContext CreateAuthorizationContext(
        bool isAuthenticated,
        string? userId,
        bool requiresPermission)
    {
        #region Prepare the request identity and endpoint metadata

        var claims = new List<Claim>();

        if (userId is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, isAuthenticated ? "Test" : null));
        var descriptor = new ActionDescriptor
        {
            EndpointMetadata = requiresPermission ? [new PermissionAttribute("training.read")] : []
        };
        var action = new ActionContext(
            new DefaultHttpContext
            {
                User = user
            },
            new RouteData(),
            descriptor,
            new ModelStateDictionary());

        return new AuthorizationFilterContext(action, new List<IFilterMetadata>());

        #endregion
    }

    private sealed class WorkflowLogger<T> : ILogger<T>
    {
        #region Capture workflow diagnostics

        public List<string> Messages { get; } = [];

        public Exception? Exception { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            #region Record the emitted outcome

            Messages.Add(formatter(state, exception));
            Exception = exception;

            #endregion
        }

        #endregion
    }
}
