using DevTrack.Features.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Api.Controllers;

[ApiController, Route("health")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class HealthController : ControllerBase
{
    [AllowAnonymous, HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        #region Report that the API is running

        return Ok(new HealthResponse("healthy"));

        #endregion
    }
}
