using System.Security.Claims;
using DevTrack.Features.Access.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Api.Controllers;

[ApiController, Route("me")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MeController(UserAccessService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MeResponse>> GetAsync(CancellationToken cancellationToken)
    {
        #region Resolve the authenticated user from current server data

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var result = await users.GetMeAsync(userId, cancellationToken);
        return result.IsSuccess ? Ok(result.Data) : Unauthorized();

        #endregion
    }
}
