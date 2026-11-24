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

            var user = loginResult.Data!;

            logger.LogInformation("Login => Authentication succeeded. UserId={UserId}", user.UserId);

            #endregion

            #region Establish the session

            var sessionResult = await EstablishSessionAsync(user);


            if (sessionResult.IsFailure)
            {
                logger.LogInformation("Login => Session creation stopped. Reason={Reason}", sessionResult.Message);
                ModelState.AddModelError(string.Empty, sessionResult.Message);

                return View(model);
            }

            logger.LogInformation("Login => Session established. UserId={UserId}", user.UserId);
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

    private async Task<Result> EstablishSessionAsync(LoginResponse user)
    {
        #region Establish Session

        var displayName = user.DisplayName;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Role, user.Role)
        };

        claims.AddRange(user.Permissions.Select(x => new Claim("permission", x)));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            IsPersistent = false
        };
        authProperties.StoreTokens(new[]
        {
            new AuthenticationToken { Name = "access_token", Value = user.AccessToken }
        });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        return Result.Success("Session established.");

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
