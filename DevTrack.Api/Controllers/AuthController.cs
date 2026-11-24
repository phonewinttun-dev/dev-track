using System.Security.Claims;
using DevTrack.Api.Auth;
using DevTrack.Features.Access.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevTrack.Shared;
using DevTrack.Database.Entities;

namespace DevTrack.Api.Controllers;

[ApiController, Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(
    RbacService service,
    JwtTokenService tokens,
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

            var userResult = await AuthenticateAsync(request);

            if (userResult.IsFailure)
            {
                logger.LogInformation("LoginAsync => Authentication stopped. Reason={Reason}", userResult.Message);

                return Unauthorized();
            }

            var user = userResult.Data!;

            logger.LogInformation("LoginAsync => Authentication succeeded. UserId={UserId}", user.Id);

            #endregion

            #region Load the user's permissions

            var permissionsResult = await LoadPermissionsAsync(user.Id);

            if (permissionsResult.IsFailure)
            {
                logger.LogWarning("LoginAsync => Permission lookup stopped. Reason={Reason}", permissionsResult.Message);

                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            var permissions = permissionsResult.Data!;

            logger.LogInformation("LoginAsync => Permissions loaded. UserId={UserId}, PermissionCount={PermissionCount}",
                user.Id, permissions.Length);

            #endregion

            #region Issue the access token

            var responseResult = IssueLoginResponse(user, permissions);

            if (responseResult.IsFailure)
            {
                logger.LogWarning("LoginAsync => Token creation stopped. Reason={Reason}", responseResult.Message);

                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            logger.LogInformation("LoginAsync => Access token issued. UserId={UserId}", user.Id);

            return Ok(responseResult.Data);

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "LoginAsync => Operation failed.");
            throw;
        }
    }

    private async Task<Result<AppUser>> AuthenticateAsync(LoginRequest request)
    {
        #region Authenticate

        var user = await service.LoginAsync(request.Username, request.Password);

        if (user is null)
        {
            return Result<AppUser>.Failure("Invalid username or password.");
        }

        return Result<AppUser>.Success(user);

        #endregion
    }

    private async Task<Result<string[]>> LoadPermissionsAsync(int userId)
    {
        #region Load Permissions

        var permissions = await service.GetPermissionsAsync(userId);

        return Result<string[]>.Success(permissions);

        #endregion
    }

    private Result<LoginResponse> IssueLoginResponse(AppUser user, string[] permissions)
    {
        #region Issue Login Response

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.RoleName)
        };
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
        var tokenResult = tokens.CreateToken(claims);

        if (tokenResult.IsFailure)
        {
            return Result<LoginResponse>.Failure(tokenResult.Message, tokenResult.ErrorCode);
        }

        var response = new LoginResponse(tokenResult.Data!, user.Id, user.DisplayName, user.Role.RoleName, permissions);

        return Result<LoginResponse>.Success(response);

        #endregion
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<string[]>> GetPermissionsAsync()
    {
        #region Get Permissions

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        return await service.GetPermissionsAsync(userId);

        #endregion
    }
}
