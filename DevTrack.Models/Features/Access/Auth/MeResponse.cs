namespace DevTrack.Features.Access.Auth;

public record MeResponse(int UserId, string Username, string DisplayName, string Role, string[] Permissions);
