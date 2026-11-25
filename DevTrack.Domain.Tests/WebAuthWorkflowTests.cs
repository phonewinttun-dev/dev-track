using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DevTrack.Features.Access.Auth;
using DevTrack.Shared;
using DevTrack.Shared.Security;
using DevTrack.WebApp.Auth;
using DevTrack.WebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevTrack.Domain.Tests;

public class WebAuthWorkflowTests
{
    [Theory]
    [InlineData(false, null, "Basic")]
    [InlineData(true, null, "Basic")]
    [InlineData(true, "  ", "Basic")]
    [InlineData(true, "private-access-token", "Bearer")]
    public async Task SendAsync_PreservesOptionalTokenBehaviorAndResponseOwnership(
        bool hasContext,
        string? token,
        string expectedScheme)
    {
        #region Prepare a request without external network access

        var authentication = new AuthenticationStub(token);
        using var services = CreateServices(authentication);
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        var content = new TrackingContent("response");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content
        };
        var transport = new Transport((_, _) => Task.FromResult(response));
        var logger = new WorkflowLogger<TokenSendHandler>();
        var handler = new TokenSendHandler(
            new HttpContextAccessor
            {
                HttpContext = hasContext ? context : null
            },
            logger)
        {
            InnerHandler = transport
        };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test/resource");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "existing-credentials");

        using var actual = await invoker.SendAsync(request, CancellationToken.None);

        #endregion

        #region Verify forwarding and diagnostic safety

        Assert.Same(response, actual);
        Assert.Equal(1, transport.Calls);
        Assert.Equal(expectedScheme, request.Headers.Authorization?.Scheme);
        Assert.Equal(expectedScheme == "Bearer" ? token : "existing-credentials",
            request.Headers.Authorization?.Parameter);
        Assert.False(content.IsDisposed);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("private-access-token"));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("existing-credentials"));

        #endregion
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ApiCall_PreservesPayloadAndDisposesOwnedResources(bool isLogin)
    {
        #region Read an isolated successful response

        var payload = isLogin
            ? JsonSerializer.Serialize(new LoginResponse("private-token"))
            : JsonSerializer.Serialize(new MeResponse(1, "private-user", "Private Name", "Reader", ["training.read"]));
        var content = new TrackingContent(payload);
        var transport = new Transport((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content
        }));
        var factory = new ClientFactory(transport);
        var logger = new WorkflowLogger<AuthApiClient>();
        var api = new AuthApiClient(factory, logger);

        var result = await CallApiAsync(api, isLogin, CancellationToken.None);

        #endregion

        #region Verify payload semantics, ownership and secret-free logs

        Assert.True(result.IsSuccess);

        if (isLogin)
        {
            Assert.Equal("private-token", Assert.IsType<Result<LoginResponse>>(result).Data?.AccessToken);
        }
        else
        {
            Assert.Equal("private-user", Assert.IsType<Result<MeResponse>>(result).Data!.Username);
        }

        Assert.Equal(isLogin ? "/api/auth/login" : "/me", transport.Path);
        Assert.Equal(isLogin ? HttpMethod.Post : HttpMethod.Get, transport.Method);
        Assert.Equal(ApiClientBase.ClientName, factory.RequestedName);
        Assert.True(content.IsDisposed);
        Assert.All(factory.Clients, client => Assert.True(client.IsDisposed));
        var logs = string.Join("\n", logger.Messages);
        Assert.DoesNotContain("private-token", logs);
        Assert.DoesNotContain("private-password", logs);
        Assert.DoesNotContain("Private Name", logs);

        #endregion
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ApiCall_UnauthorizedReturnsFailureWithoutReadingInvalidJson(bool isLogin)
    {
        #region Return an unauthorized response with an unreadable body

        var content = new TrackingContent("invalid-json");
        var factory = new ClientFactory(new Transport((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = content
            })));
        var api = new AuthApiClient(factory, NullLogger<AuthApiClient>.Instance);

        var result = await CallApiAsync(api, isLogin, CancellationToken.None);

        #endregion

        #region Verify the unauthorized Result and cleanup

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrorCodes.Unauthorized, result.ErrorCode);
        Assert.True(content.IsDisposed);
        Assert.All(factory.Clients, client => Assert.True(client.IsDisposed));

        #endregion
    }

    [Theory]
    [InlineData(true, 500, "{}", AuthErrorCodes.ApiFailure)]
    [InlineData(false, 403, "{}", AuthErrorCodes.ApiFailure)]
    [InlineData(true, 200, "invalid-json", AuthErrorCodes.InvalidResponse)]
    [InlineData(false, 200, "invalid-json", AuthErrorCodes.InvalidResponse)]
    [InlineData(false, 200, "{}", AuthErrorCodes.InvalidResponse)]
    [InlineData(false, 200, "{\"userId\":1,\"username\":\"test\",\"displayName\":\"Test\",\"role\":\"Reader\",\"permissions\":null}", AuthErrorCodes.InvalidResponse)]
    public async Task ApiCall_ReturnsFailureAndDisposesOwnedResources(
        bool isLogin,
        int status,
        string body,
        string errorCode)
    {
        #region Fail status validation or payload reading

        var content = new TrackingContent(body);
        var factory = new ClientFactory(new Transport((_, _) => Task.FromResult(
            new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = content
            })));
        var logger = new WorkflowLogger<AuthApiClient>();
        var api = new AuthApiClient(factory, logger);

        var result = await CallApiAsync(api, isLogin, CancellationToken.None);

        #endregion

        #region Verify the failure classification and cleanup

        Assert.True(result.IsFailure);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Null(logger.Exception);
        Assert.True(content.IsDisposed);
        Assert.All(factory.Clients, client => Assert.True(client.IsDisposed));

        #endregion
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ApiCall_EmptyPayloadReturnsFailureAndDisposesOwnedResources(bool isLogin)
    {
        #region Read an empty API payload

        var content = new TrackingContent("null");
        var factory = new ClientFactory(new Transport((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            })));
        var logger = new WorkflowLogger<AuthApiClient>();
        var api = new AuthApiClient(factory, logger);

        var result = await CallApiAsync(api, isLogin, CancellationToken.None);

        #endregion

        #region Verify the failed Result is returned without throwing or leaking resources

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrorCodes.InvalidResponse, result.ErrorCode);
        Assert.Equal(isLogin
            ? "The API returned an empty login response."
            : "The API returned an empty current user response.", result.Message);
        Assert.Null(logger.Exception);
        Assert.True(content.IsDisposed);
        Assert.All(factory.Clients, client => Assert.True(client.IsDisposed));

        #endregion
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ApiCall_PropagatesCancellationAndDisposesClient(bool isLogin)
    {
        #region Cancel during the outgoing request

        using var cancellation = new CancellationTokenSource();
        var factory = new ClientFactory(new Transport((_, token) =>
        {
            cancellation.Cancel();

            return Task.FromCanceled<HttpResponseMessage>(token);
        }));
        var logger = new WorkflowLogger<AuthApiClient>();
        var api = new AuthApiClient(factory, logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CallApiAsync(api, isLogin, cancellation.Token));

        #endregion

        #region Verify cancellation is not converted into an auth failure

        Assert.Null(logger.Exception);
        Assert.All(factory.Clients, client => Assert.True(client.IsDisposed));

        #endregion
    }

    [Theory]
    [InlineData(false, 200, "{\"userId\":1,\"username\":\"test-user\",\"displayName\":\"Test User\",\"role\":\"Reader\",\"permissions\":[\"TRAINING.READ\"]}", "training.read", typeof(ChallengeResult), 0)]
    [InlineData(true, 401, "invalid-json", "training.read", typeof(ChallengeResult), 1)]
    [InlineData(true, 200, "{\"userId\":1,\"username\":\"test-user\",\"displayName\":\"Test User\",\"role\":\"Reader\",\"permissions\":[]}", "training.read", typeof(ForbidResult), 0)]
    [InlineData(true, 200, "{\"userId\":1,\"username\":\"test-user\",\"displayName\":\"Test User\",\"role\":\"Reader\",\"permissions\":[\"TRAINING.READ\"]}", "training.read", null, 0)]
    [InlineData(true, 200, "{\"userId\":1,\"username\":\"test-user\",\"displayName\":\"Test User\",\"role\":\"Reader\",\"permissions\":[\"TRAINING.READ\"]}", null, null, 0)]
    [InlineData(true, 200, "null", "training.read", typeof(StatusCodeResult), 0)]
    [InlineData(true, 200, "invalid-json", "training.read", typeof(StatusCodeResult), 0)]
    public async Task OnAuthorizationAsync_PreservesSessionAndPermissionDecisions(
        bool isAuthenticated,
        int status,
        string body,
        string? requiredPermission,
        Type? expectedResult,
        int expectedSignOuts)
    {
        #region Prepare current API permissions and the default sign-out service

        var authentication = new AuthenticationStub(null);
        using var services = CreateServices(authentication);
        var transport = new Transport((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new TrackingContent(body)
        }));
        var factory = new ClientFactory(transport);
        var api = new AuthApiClient(factory, NullLogger<AuthApiClient>.Instance);
        var filter = new PermissionFilter(api, new CurrentUser(), NullLogger<PermissionFilter>.Instance);
        var context = CreateAuthorizationContext(services, isAuthenticated, requiredPermission);

        await filter.OnAuthorizationAsync(context);

        #endregion

        #region Verify unchanged request outcomes and case-insensitive matching

        Assert.Equal(expectedResult, context.Result?.GetType());
        if (context.Result is StatusCodeResult failureResponse)
        {
            Assert.Equal(StatusCodes.Status500InternalServerError, failureResponse.StatusCode);
        }

        Assert.Equal(expectedSignOuts, authentication.SignOuts);
        Assert.Null(authentication.SignOutScheme);
        Assert.Equal(isAuthenticated ? 1 : 0, transport.Calls);
        Assert.All(factory.Clients, client => Assert.True(client.IsDisposed));

        #endregion
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnAuthorizationAsync_ChecksAllRequirementsWithOneCurrentUserLookup(bool hasEveryPermission)
    {
        var profile = new MeResponse(1, "test-user", "Test User", "Reader", ["training.read", "batches.read"]);
        var transport = new Transport((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new TrackingContent(JsonSerializer.Serialize(profile))
        }));
        var authentication = new AuthenticationStub(null);
        using var services = CreateServices(authentication);
        var factory = new ClientFactory(transport);
        var api = new AuthApiClient(factory, NullLogger<AuthApiClient>.Instance);
        var currentUser = new CurrentUser();
        var filter = new PermissionFilter(api, currentUser, NullLogger<PermissionFilter>.Instance);
        var context = CreateAuthorizationContext(services, true, null);
        context.ActionDescriptor.EndpointMetadata =
        [
            new PermissionAttribute("TRAINING.READ"),
            new PermissionAttribute(hasEveryPermission ? "batches.read" : "batches.write")
        ];

        await filter.OnAuthorizationAsync(context);

        Assert.Equal(1, transport.Calls);
        Assert.Equal("/me", transport.Path);
        Assert.NotNull(currentUser.Details);
        Assert.Equal(profile.Username, currentUser.Details.Username);
        Assert.True(currentUser.HasPermission("training.READ"));
        Assert.Equal(hasEveryPermission ? null : typeof(ForbidResult), context.Result?.GetType());
        Assert.All(factory.Clients, client => Assert.True(client.IsDisposed));
    }

    [Fact]
    public async Task OnAuthorizationAsync_SkipsCurrentUserLookupForAnonymousEndpoints()
    {
        var authentication = new AuthenticationStub(null);
        using var services = CreateServices(authentication);
        var transport = new Transport((_, _) => throw new InvalidOperationException("Anonymous pages must not call the API."));
        var api = new AuthApiClient(new ClientFactory(transport), NullLogger<AuthApiClient>.Instance);
        var user = new CurrentUser();
        var filter = new PermissionFilter(api, user, NullLogger<PermissionFilter>.Instance);
        var context = CreateAuthorizationContext(services, false, null);
        context.ActionDescriptor.EndpointMetadata = [new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute()];

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        Assert.Null(user.Details);
        Assert.Equal(0, transport.Calls);
    }

    [Fact]
    public async Task OnAuthorizationAsync_MapsApiFailureToServerFailureWithoutDenialOrSignOut()
    {
        #region Fail a required permission API request

        var authentication = new AuthenticationStub(null);
        using var services = CreateServices(authentication);
        var factory = new ClientFactory(new Transport((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError))));
        var api = new AuthApiClient(factory, NullLogger<AuthApiClient>.Instance);
        var logger = new WorkflowLogger<PermissionFilter>();
        var filter = new PermissionFilter(api, new CurrentUser(), logger);
        var context = CreateAuthorizationContext(services, true, "training.read");

        await filter.OnAuthorizationAsync(context);

        #endregion

        #region Verify a failed lookup does not become a permission denial or sign-out

        Assert.Null(logger.Exception);
        var response = Assert.IsType<StatusCodeResult>(context.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, response.StatusCode);
        Assert.Equal(0, authentication.SignOuts);

        #endregion
    }

    [Fact]
    public async Task OnAuthorizationAsync_PropagatesUnexpectedTransportExceptions()
    {
        #region Fail the transport before any API response exists

        var authentication = new AuthenticationStub(null);
        using var services = CreateServices(authentication);
        var failure = new HttpRequestException("Transport unavailable.");
        var factory = new ClientFactory(new Transport((_, _) => Task.FromException<HttpResponseMessage>(failure)));
        var api = new AuthApiClient(factory, NullLogger<AuthApiClient>.Instance);
        var logger = new WorkflowLogger<PermissionFilter>();
        var filter = new PermissionFilter(api, new CurrentUser(), logger);
        var context = CreateAuthorizationContext(services, true, "training.read");

        var thrown = await Assert.ThrowsAsync<HttpRequestException>(() => filter.OnAuthorizationAsync(context));

        #endregion

        #region Verify actual exceptions still propagate without fake authentication outcomes

        Assert.Same(failure, thrown);
        Assert.Same(failure, logger.Exception);
        Assert.Null(context.Result);
        Assert.Equal(0, authentication.SignOuts);
        Assert.All(factory.Clients, client => Assert.True(client.IsDisposed));

        #endregion
    }

    [Theory]
    [InlineData(401, "Invalid username or password.")]
    [InlineData(200, "The API returned an empty login response.")]
    [InlineData(500, "The authentication API request failed. Please try again.")]
    public async Task Login_ReturnsTheFailureReasonToTheFormWithoutStartingASession(
        int status,
        string expectedReason)
    {
        #region Return a failed login outcome from the API client

        var factory = new ClientFactory(new Transport((_, _) => Task.FromResult(
            new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new TrackingContent("null")
            })));
        var api = new AuthApiClient(factory, NullLogger<AuthApiClient>.Instance);
        var controller = new DevTrack.WebApp.Controllers.AccountController(
            api, NullLogger<DevTrack.WebApp.Controllers.AccountController>.Instance);
        var model = new DevTrack.WebApp.Models.LoginViewModel
        {
            Username = "private-user",
            Password = "private-password"
        };

        var response = await controller.Login(model, CancellationToken.None);

        #endregion

        #region Verify the workflow stops and shows the correct reason

        Assert.Same(model, Assert.IsType<ViewResult>(response).Model);
        Assert.Contains(controller.ModelState.Values.SelectMany(value => value.Errors),
            error => error.ErrorMessage == expectedReason);
        Assert.All(factory.Clients, client => Assert.True(client.IsDisposed));

        #endregion
    }

    private static async Task<Result> CallApiAsync(AuthApiClient api, bool isLogin, CancellationToken token)
    {
        #region Invoke the existing public contract

        if (isLogin)
        {
            return await api.LoginAsync(new LoginRequest("private-user", "private-password"), token);
        }

        return await api.GetMeAsync(token);

        #endregion
    }

    private static ServiceProvider CreateServices(AuthenticationStub authentication)
    {
        #region Register isolated authentication behavior

        return new ServiceCollection()
            .AddSingleton<IAuthenticationService>(authentication)
            .BuildServiceProvider();

        #endregion
    }

    private static AuthorizationFilterContext CreateAuthorizationContext(
        IServiceProvider services,
        bool isAuthenticated,
        string? permission)
    {
        #region Create request identity and metadata

        var context = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(isAuthenticated ? "Test" : null))
        };
        var descriptor = new ActionDescriptor
        {
            EndpointMetadata = permission is null ? [] : [new PermissionAttribute(permission)]
        };
        var action = new ActionContext(context, new RouteData(), descriptor, new ModelStateDictionary());

        return new AuthorizationFilterContext(action, new List<IFilterMetadata>());

        #endregion
    }

    private sealed class ClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        #region Track factory clients and their lifetimes

        public List<TrackingClient> Clients { get; } = [];

        public string? RequestedName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            RequestedName = name;
            var client = new TrackingClient(handler)
            {
                BaseAddress = new Uri("https://api.test/")
            };
            Clients.Add(client);

            return client;
        }

        #endregion
    }

    private sealed class TrackingClient(HttpMessageHandler handler) : HttpClient(handler, false)
    {
        #region Observe client disposal

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }

    private sealed class TrackingContent(string value) : StringContent(value, Encoding.UTF8, "application/json")
    {
        #region Observe response content disposal

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }

        #endregion
    }

    private sealed class Transport(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        #region Record isolated outgoing requests

        public int Calls { get; private set; }

        public string? Path { get; private set; }

        public HttpMethod? Method { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            Path = request.RequestUri?.AbsolutePath;
            Method = request.Method;

            return await send(request, cancellationToken);
        }

        #endregion
    }

    private sealed class AuthenticationStub(string? token) : IAuthenticationService
    {
        #region Provide token storage and default sign-out behavior

        public int SignOuts { get; private set; }

        public string? SignOutScheme { get; private set; }

        public async Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            var properties = new AuthenticationProperties();
            properties.StoreTokens([new AuthenticationToken
            {
                Name = "access_token",
                Value = token!
            }]);

            return await Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity("Test")), properties, "Test")));
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            throw new InvalidOperationException("The filter must return a challenge result without executing it.");
        }

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            throw new InvalidOperationException("The filter must return a forbid result without executing it.");
        }

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties)
        {
            throw new InvalidOperationException("Authorization must not sign in.");
        }

        public async Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignOuts++;
            SignOutScheme = scheme;

            await Task.CompletedTask;
        }

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
            Messages.Add(formatter(state, exception));
            Exception = exception;
        }

        #endregion
    }
}
