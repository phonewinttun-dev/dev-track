namespace DevTrack.Features.Access.Auth;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string AccessToken);
