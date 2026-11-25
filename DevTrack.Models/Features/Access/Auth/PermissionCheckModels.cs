namespace DevTrack.Features.Access.Auth;

public record PermissionCheckRequest(string[] RequiredPermissions);
public record PermissionCheckResponse(bool IsAllowed);
