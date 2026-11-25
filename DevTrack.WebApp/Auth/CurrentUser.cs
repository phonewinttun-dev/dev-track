using DevTrack.Features.Access.Auth;

namespace DevTrack.WebApp.Auth;

public class CurrentUser
{
    public MeResponse? Details { get; set; }

    public bool HasPermission(string permission)
    {
        #region Use only the current request's server-loaded permissions

        return Details is not null && Details.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

        #endregion
    }
}
