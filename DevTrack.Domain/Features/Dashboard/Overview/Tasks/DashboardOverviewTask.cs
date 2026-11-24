using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Dashboard;

public partial class DashboardService
{
    private async Task<Result<int>> CountBatchesAsync()
    {
        #region Count Batches

        var totalBatches = await dbContext.Batches.CountAsync();

        return Result<int>.Success(totalBatches, "Batches counted.");

        #endregion
    }

    private async Task<Result<int>> CountDevelopersAsync()
    {
        #region Count Developers

        var totalDevelopers = await dbContext.Developers.CountAsync();

        return Result<int>.Success(totalDevelopers, "Developers counted.");

        #endregion
    }

    private async Task<Result<int>> CountActiveBatchesAsync()
    {
        #region Count Active Batches

        var today = DateOnly.FromDateTime(DateTime.Today);
        var activeBatches = await dbContext.Batches.CountAsync(b => b.StartDate <= today && b.EndDate >= today);

        return Result<int>.Success(activeBatches, "Active batches counted.");

        #endregion
    }

    private async Task<Result<decimal>> CalculateAttendanceRateAsync()
    {
        #region Calculate Attendance Rate

        var attendance = await dbContext.AttendanceRecords
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Present = group.Count(record => record.Status == "Present")
            })
            .SingleOrDefaultAsync();
        var overallRate = attendance is not null
            ? (decimal)attendance.Present * 100 / attendance.Total
            : 0;

        return Result<decimal>.Success(overallRate, "Overall attendance calculated.");

        #endregion
    }

    private async Task<Result<List<BatchSummaryModel>>> LoadRecentBatchesAsync()
    {
        #region Load Recent Batches

        var recentBatches = await dbContext.Batches
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .Select(b => new BatchSummaryModel
            {
                Id = b.Id,
                BatchName = b.BatchName,
                MentorName = b.MentorName ?? "Unassigned",
                DeveloperCount = dbContext.BatchDevelopers.Count(bd => bd.BatchId == b.Id)
            })
            .ToListAsync();

        return Result<List<BatchSummaryModel>>.Success(recentBatches, "Recent batches loaded.");

        #endregion
    }

    private async Task<Result<List<AttendanceTrendModel>>> LoadAttendanceTrendsAsync()
    {
        #region Load Attendance Trends

        // Trend for last 7 active days
        var trends = await dbContext.AttendanceRecords
            .GroupBy(a => a.TrainingDate)
            .OrderByDescending(g => g.Key)
            .Take(7)
            .Select(g => new
            {
                Date = g.Key,
                PresentCount = g.Count(a => a.Status == "Present")
            })
            .ToListAsync();

        var result = trends
            .AsEnumerable()
            .Reverse()
            .Select(trend => new AttendanceTrendModel
        {
            Date = trend.Date.ToString("MM/dd"),
            PresentCount = trend.PresentCount
            })
            .ToList();

        return Result<List<AttendanceTrendModel>>.Success(result, "Attendance trends loaded.");

        #endregion
    }
}
