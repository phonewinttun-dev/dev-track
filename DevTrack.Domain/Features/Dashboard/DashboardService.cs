using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace DevTrack.Features.TrainingManagement.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly DevTrackDbContext _db;

    private readonly ILogger<DashboardService> logger;

    public DashboardService(DevTrackDbContext db, ILogger<DashboardService> logger)
    {
        _db = db;
        this.logger = logger;
    }

    public async Task<Result<DashboardResponse>> GetDashboardDataAsync()
    {
        try
        {
            var totalBatches = await _db.Batches.CountAsync();
            var totalDevelopers = await _db.Developers.CountAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var activeBatches = await _db.Batches.CountAsync(b => b.StartDate <= today && b.EndDate >= today);

            var attendance = await _db.AttendanceRecords
                .GroupBy(_ => 1)
                .Select(group => new { Total = group.Count(), Present = group.Count(record => record.Status == "Present") })
                .SingleOrDefaultAsync();
            var overallRate = attendance is not null
                ? (decimal)attendance.Present * 100 / attendance.Total
                : 0;

            var recentBatches = await _db.Batches
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new BatchSummaryModel
                {
                    Id = b.Id,
                    BatchName = b.BatchName,
                    MentorName = b.MentorName ?? "Unassigned",
                    DeveloperCount = _db.BatchDevelopers.Count(bd => bd.BatchId == b.Id)
                })
                .ToListAsync();

            // Trend for last 7 active days
            var trends = await _db.AttendanceRecords
                .GroupBy(a => a.TrainingDate)
                .OrderByDescending(g => g.Key)
                .Take(7)
                .Select(g => new
                {
                    Date = g.Key,
                    PresentCount = g.Count(a => a.Status == "Present")
                })
                .ToListAsync();

            var response = new DashboardResponse
            {
                TotalBatches = totalBatches,
                TotalDevelopers = totalDevelopers,
                ActiveBatches = activeBatches,
                OverallAttendanceRate = Math.Round(overallRate, 1),
                RecentBatches = recentBatches,
                AttendanceTrends = trends.AsEnumerable().Reverse().Select(trend => new AttendanceTrendModel
                {
                    Date = trend.Date.ToString("MM/dd"),
                    PresentCount = trend.PresentCount
                }).ToList()
            };

            return Result<DashboardResponse>.Success(response);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetDashboardDataAsync => Operation failed.");
            return Result<DashboardResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<Result<List<BatchSummaryModel>>> GetDashboardReportAsync()
    {
        try
        {
            var batches = await _db.Batches
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BatchSummaryModel
                {
                    Id = b.Id,
                    BatchName = b.BatchName,
                    MentorName = b.MentorName ?? "Unassigned",
                    DeveloperCount = _db.BatchDevelopers.Count(bd => bd.BatchId == b.Id)
                })
                .ToListAsync();

            return Result<List<BatchSummaryModel>>.Success(batches);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetDashboardReportAsync => Operation failed.");
            return Result<List<BatchSummaryModel>>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
