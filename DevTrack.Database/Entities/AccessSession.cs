namespace DevTrack.Database.Entities;

public class AccessSession
{
    public string TokenHash { get; set; } = string.Empty;
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
}
