using Microsoft.AspNetCore.Authentication;

namespace DevTrack.WebApp.Auth;

public partial class PermissionFilter
{
    private static async Task SignOutSessionAsync(HttpContext context)
    {
        #region End the existing session using the default scheme

        await context.SignOutAsync();

        #endregion
    }
}
