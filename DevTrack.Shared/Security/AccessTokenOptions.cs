namespace DevTrack.Shared.Security;

public class AccessTokenOptions
{
    public const string SectionName = "AccessToken";
    public string SigningKey { get; set; } = string.Empty;
    public string KeyFile { get; set; } = "devtrack.key";
    public int ExpiresMinutes { get; set; } = 480;
}
