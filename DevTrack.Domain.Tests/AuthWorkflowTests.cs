using DevTrack.Api.Auth;
using System.Security.Claims;
using DevTrack.Api.Auth.Authorize.Tasks;
using DevTrack.Features.Access.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevTrack.Domain.Tests;

public class AuthWorkflowTests
{
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
        var service = new UserAccessService(db, NullLogger<UserAccessService>.Instance);
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
        var filter = new PermissionFilter(new UserAccessService(db, NullLogger<UserAccessService>.Instance), logger);
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
