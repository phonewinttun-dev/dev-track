using Microsoft.AspNetCore.Mvc;

namespace DevTrack.Api.Auth.Authorize.Tasks;

public class PermissionAttribute(string permission) : TypeFilterAttribute(typeof(PermissionFilter))
{
    #region Required permission metadata

    public string Permission { get; } = permission;

    #endregion
}
