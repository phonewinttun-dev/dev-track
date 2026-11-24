using DevTrack.Shared;
using Microsoft.AspNetCore.Authentication;

namespace DevTrack.WebApp.Auth;

public partial class PermissionFilter
{
    private static async Task<Result> SignOutSessionAsync(HttpContext context)
    {
        #region End the existing session using the default scheme

        await context.SignOutAsync();

        return Result.Success("Session signed out.");

        #endregion
    }
}
