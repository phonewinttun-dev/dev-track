namespace DevTrack.Domain.Tests;

internal static class AuthTestSettings
{
    public const string SigningKey = "hVJGPNY0spWbQq5QHGu5s5lRC/6ANVlVyC5yK23g62U=";
    public const string Username = "admin";
    public const string Password = "admin@123";
    public const string DisplayName = "System Admin";

    public static IReadOnlyDictionary<string, string?> ApiConfiguration => new Dictionary<string, string?>
    {
        ["DatabaseProvider"] = "InMemory",
        ["AccessToken:ExpiresMinutes"] = "480",
        ["AccessToken:SigningKey"] = SigningKey
    };
}
