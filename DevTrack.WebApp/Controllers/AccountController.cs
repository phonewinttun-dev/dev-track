using System.Security.Cryptography;
using DevTrack.Features.Access.Auth;
using DevTrack.Shared;
using DevTrack.Shared.Security;
using System.Security.Claims;
using DevTrack.WebApp.Auth;
using DevTrack.WebApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.WebApp.Controllers;

public class AccountController(AuthApiClient authApi, ILogger<AccountController> logger) : Controller
{
    private const int SessionNonceSize = 32;
    private const int SessionLifetimeHours = 8;
    private const string SessionNonceClaim = "sub";
    private const string AccessTokenName = "access_token";

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        #region Login

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectAfterLogin(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl ?? Url.Content("~/") });

        #endregion
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            #region Authenticate the user

            var loginResult = await AuthenticateUserAsync(model, cancellationToken);

            if (loginResult.IsFailure)
            {
                logger.LogInformation("Login => Authentication stopped. Reason={Reason}", loginResult.Message);
                ModelState.AddModelError(string.Empty, loginResult.Message);

                return View(model);
            }

            var loginResponse = loginResult.Data!;

            logger.LogInformation("Login => Authentication succeeded.");

            #endregion

            #region Establish the session

            await EstablishSessionAsync(loginResponse);

            logger.LogInformation("Login => Session established.");
            TempData["WorkflowSuccess"] = "Signed in successfully.";

            return RedirectAfterLogin(model.ReturnUrl);

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Login => Operation failed.");
            throw;
        }
    }

    private async Task<Result<LoginResponse>> AuthenticateUserAsync(LoginViewModel model, CancellationToken cancellationToken)
    {
        #region Authenticate User

        var loginResult = await authApi.LoginAsync(new LoginRequest(model.Username, model.Password), cancellationToken);

        if (loginResult.IsFailure)
        {
            var reason = loginResult.ErrorCode == AuthErrorCodes.Unauthorized
                ? "Invalid username or password."
                : loginResult.Message;

            return Result<LoginResponse>.Failure(reason, loginResult.ErrorCode);
        }

        return loginResult;

        #endregion
    }

    private async Task EstablishSessionAsync(LoginResponse loginResponse)
    {
        #region Establish Session

        // Antiforgery binds forms to this independent session nonce, never to a user ID.
        var sessionNonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(SessionNonceSize));
        var identity = new ClaimsIdentity(
            [new Claim(SessionNonceClaim, sessionNonce)], CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(SessionLifetimeHours),
            IsPersistent = false
        };
        authProperties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = AccessTokenName, Value = loginResponse.AccessToken }
        });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        #endregion
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        #region Logout

        try
        {
            var logoutResult = await EndSessionAsync();

            if (logoutResult.IsFailure)
            {
                logger.LogInformation("Logout => Sign out stopped. Reason={Reason}", logoutResult.Message);

                return View("WorkflowFailure", logoutResult.Message);
            }

            logger.LogInformation("Logout => Sign out succeeded.");
            TempData["WorkflowSuccess"] = logoutResult.Message;

            return RedirectToAction(nameof(Login));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Logout => Operation failed.");
            throw;
        }

        #endregion
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Denied()
    {
        #region Denied

        return View();

        #endregion
    }

    private async Task<Result> EndSessionAsync()
    {
        #region End Session

        var revokeResult = await authApi.LogoutAsync(HttpContext.RequestAborted);
        if (revokeResult.IsFailure && revokeResult.ErrorCode != AuthErrorCodes.Unauthorized)
        {
            return revokeResult;
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Result.Success("Signed out successfully.");

        #endregion
    }

    private IActionResult RedirectAfterLogin(string? returnUrl)
    {
        #region Redirect After Login

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");

        #endregion
    }
}
