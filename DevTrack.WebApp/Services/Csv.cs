namespace DevTrack.WebApp.Services;

internal static class Csv
{
    public static string Escape(string? value)
    {
        return $"\"{value?.Replace("\"", "\"\"")}\"";
    }
}
