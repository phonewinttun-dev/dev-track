namespace DevTrack.WebApp.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class PermissionAttribute(string permission) : Attribute
{
    public string Permission { get; } = permission;
}
