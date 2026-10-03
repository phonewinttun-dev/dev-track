namespace DevTrack.Shared.Security;

public static class Permissions
{
    public const string DashboardRead = "dashboard.read";
    public const string BatchesRead = "batches.read";
    public const string BatchesWrite = "batches.write";
    public const string DevelopersRead = "developers.read";
    public const string DevelopersWrite = "developers.write";
    public const string TrainingRead = "training.read";
    public const string TrainingWrite = "training.write";
    public static readonly string[] All = [DashboardRead, BatchesRead, BatchesWrite, DevelopersRead, DevelopersWrite, TrainingRead, TrainingWrite];
}
public record LoginRequest(string Username, string Password);
public record LoginResponse(string AccessToken, int UserId, string DisplayName, string Role, string[] Permissions);
