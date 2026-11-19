using DevTrack.Shared.Security;

namespace DevTrack.Domain.Tests;

internal static class AuthTestSettings
{
    public const string Username = "admin";
    public const string Password = "correct-password";
    public const string DisplayName = "System Admin";
    public const string Issuer = "DevTrack.Tests";
    public const string Audience = "DevTrack.Api.Tests";
    public const string SigningKey = "devtrack-tests-signing-key-with-32-plus-chars";

    public static IReadOnlyDictionary<string, string?> ApiConfiguration => new Dictionary<string, string?>
    {
        ["DatabaseProvider"] = "InMemory",
        ["AdminAuth:Username"] = Username,
        ["AdminAuth:Password"] = Password,
        ["AdminAuth:DisplayName"] = DisplayName,
        [$"{JwtOptions.SectionName}:Issuer"] = Issuer,
        [$"{JwtOptions.SectionName}:Audience"] = Audience,
        [$"{JwtOptions.SectionName}:SigningKey"] = SigningKey,
        [$"{JwtOptions.SectionName}:ExpiresMinutes"] = "480"
    };
}
