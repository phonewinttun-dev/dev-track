using DevTrack.Database.Entities;

namespace DevTrack.Features.TrainingManagement.Training;

internal sealed class AttendanceMetrics
{
    private const int DefaultEligiblePercent = 80;
    private const decimal WarningPercent = 70;

    public int PresentDays { get; private set; }
    public int AbsentDays { get; private set; }
    public int LeaveDays { get; private set; }
    public int LateDays { get; private set; }
    public decimal AttendancePercent { get; private set; }
    public string Status { get; private set; } = "Not Eligible";

    public static AttendanceMetrics Calculate(IEnumerable<AttendanceRecord> attendance, int totalClasses, int? minAttendancePercent)
    {
        var metrics = new AttendanceMetrics();

        foreach (var record in attendance)
        {
            switch (record.Status)
            {
                case "Present":
                    metrics.PresentDays++;
                    break;
                case "Absent":
                    metrics.AbsentDays++;
                    break;
                case "Leave":
                    metrics.LeaveDays++;
                    break;
                case "Late":
                    metrics.LateDays++;
                    break;
            }
        }

        var percent = totalClasses > 0 ? (decimal)metrics.PresentDays * 100 / totalClasses : 0;
        metrics.AttendancePercent = Math.Round(percent, 2);
        metrics.Status = GetStatus(percent, minAttendancePercent ?? DefaultEligiblePercent);
        return metrics;
    }

    private static string GetStatus(decimal percent, int eligiblePercent)
    {
        if (percent >= eligiblePercent)
        {
            return "Eligible";
        }

        return percent >= WarningPercent ? "Warning" : "Not Eligible";
    }
}
