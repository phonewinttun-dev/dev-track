using System.Security.Claims;
using DevTrack.Shared;

namespace DevTrack.Api.Auth;

public partial class PermissionFilter
{
    private static Result CheckAuthentication(ClaimsPrincipal user)
    {
        #region Require the existing authenticated identity

        if (user.Identity?.IsAuthenticated != true)
        {
            return Result.Failure("An authenticated identity is required.");
        }

        return Result.Success("Identity authenticated.");

        #endregion
    }

    private static Result<int> ReadUserId(ClaimsPrincipal user)
    {
        #region Read the existing numeric identifier

        if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Result<int>.Failure("The user identifier is missing or invalid.");
        }

        return Result<int>.Success(userId, "User identifier read.");

        #endregion
    }
}
