using System.Security.Claims;
using DevTrack.Api.Auth;
using DevTrack.Domain.Security;
using DevTrack.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DevTrack.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(RbacService service, JwtTokenService tokens) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password)) return BadRequest();
        var user = await service.LoginAsync(request.Username, request.Password);
        if (user is null) return Unauthorized();
        var permissions = await service.GetPermissionsAsync(user.Id);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.DisplayName), new(ClaimTypes.Role, user.Role.RoleName) };
        claims.AddRange(permissions.Select(x => new Claim("permission", x)));
        return Ok(new LoginResponse(tokens.CreateToken(claims), user.Id, user.DisplayName, user.Role.RoleName, permissions));
    }
}
