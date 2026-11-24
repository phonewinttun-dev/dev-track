using Microsoft.AspNetCore.Mvc;

namespace DevTrack.WebApp.Auth;

public class PermissionAttribute(string permission) : TypeFilterAttribute(typeof(PermissionFilter))
{
    #region Required permission metadata

    public string Permission { get; } = permission;

    #endregion
}
