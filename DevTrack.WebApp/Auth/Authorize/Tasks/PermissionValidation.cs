using System.Security.Claims;
using DevTrack.Shared;

namespace DevTrack.WebApp.Auth;

public partial class PermissionFilter
{
    private static Result CheckAuthentication(ClaimsPrincipal user)
    {
        #region Require an authenticated identity

        if (user.Identity?.IsAuthenticated != true)
        {
            return Result.Failure("An authenticated identity is required.");
        }

        return Result.Success("Identity authenticated.");

        #endregion
    }
}
