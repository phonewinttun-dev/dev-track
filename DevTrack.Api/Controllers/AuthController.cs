using System.Security.Claims;
using DevTrack.Api.Auth;
using DevTrack.Features.Access.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Api.Controllers;

[ApiController, Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(RbacService service, JwtTokenService tokens) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest();
        }

        var user = await service.LoginAsync(request.Username, request.Password);
        if (user is null)
        {
            return Unauthorized();
        }

        var permissions = await service.GetPermissionsAsync(user.Id);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.RoleName)
        };
        claims.AddRange(permissions.Select(x => new Claim("permission", x)));
        return Ok(new LoginResponse(tokens.CreateToken(claims), user.Id, user.DisplayName, user.Role.RoleName, permissions));
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<string[]>> GetPermissionsAsync()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        return await service.GetPermissionsAsync(userId);
    }
}
