using System.Security.Claims;
using DevTrack.Features.Access.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Api.Controllers;

[ApiController, Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(
    UserAccessService service,
    SessionService tokens,
    ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> LoginAsync(LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest();
            }

            #region Authenticate the credentials

            var user = await service.LoginAsync(request.Username, request.Password);

            if (user is null)
            {
                logger.LogInformation("LoginAsync => Credentials rejected.");

                return Unauthorized();
            }

            logger.LogInformation("LoginAsync => Authentication succeeded. UserId={UserId}", user.Id);

            #endregion

            #region Issue the access token

            var tokenResult = await tokens.CreateAsync(user.Id, HttpContext.RequestAborted);

            if (tokenResult.IsFailure)
            {
                logger.LogWarning("LoginAsync => Token creation stopped. Reason={Reason}", tokenResult.Message);

                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("LoginAsync => Access token issued. UserId={UserId}", user.Id);

            return Ok(new LoginResponse(tokenResult.Data!));

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "LoginAsync => Operation failed.");
            throw;
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        #region Revoke the authenticated session

        var header = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(Request.Headers.Authorization.ToString());
        var result = await tokens.RevokeAsync(header.Parameter!, cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(StatusCodes.Status500InternalServerError);

        #endregion
    }

    [HttpPost("permission-check")]
    public async Task<ActionResult<PermissionCheckResponse>> CheckPermissionsAsync(
        PermissionCheckRequest request, CancellationToken cancellationToken)
    {
        try
        {
            #region Validate the permission check request

            if (request.RequiredPermissions is null || request.RequiredPermissions.Any(string.IsNullOrWhiteSpace))
            {
                return BadRequest();
            }

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            #endregion

            #region Evaluate current grants without returning them

            var result = await service.CheckPermissionsAsync(userId, request, cancellationToken);
            if (result.IsFailure)
            {
                logger.LogWarning("CheckPermissionsAsync => Permission check stopped. Reason={Reason}", result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok(result.Data);

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "CheckPermissionsAsync => Permission check failed.");
            throw;
        }
    }
}
