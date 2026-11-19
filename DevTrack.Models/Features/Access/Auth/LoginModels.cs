namespace DevTrack.Features.Access.Auth;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string AccessToken, int UserId, string DisplayName, string Role, string[] Permissions);
