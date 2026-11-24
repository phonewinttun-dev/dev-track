using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    private async Task<Result<int?>> GetMinAttendancePercentAsync(int batchId)
    {
        #region Get Min Attendance Percent

        var minimum = await dbContext.Batches
            .Where(batch => batch.Id == batchId)
            .Select(batch => batch.MinAttendancePercent)
            .SingleOrDefaultAsync();

        return Result<int?>.Success(minimum, "Minimum attendance loaded.");

        #endregion
    }

    private static AttendanceSummaryResponse CreateAttendanceSummary(
        SummaryDeveloper developer,
        IEnumerable<AttendanceRecord> attendance,
        int totalClasses,
        int? minAttendancePercent)
    {
        #region Create Attendance Summary

        var metrics = AttendanceMetrics.Calculate(attendance, totalClasses, minAttendancePercent);
        return new AttendanceSummaryResponse
        {
            DeveloperId = developer.Id,
            FullName = developer.FullName,
            DeveloperCode = developer.DeveloperCode,
            TotalClasses = totalClasses,
            PresentDays = metrics.PresentDays,
            AbsentDays = metrics.AbsentDays,
            LeaveDays = metrics.LeaveDays,
            AttendancePercent = metrics.AttendancePercent,
            Status = metrics.Status
        };

        #endregion
    }

    private async Task<Result<int>> CountRequiredClassesAsync(int batchId)
    {
        #region Count Required Classes

        var totalClasses = await dbContext.TrainingCalendars
            .CountAsync(c => c.BatchId == batchId && (c.IsAttendanceRequired ?? false));

        return Result<int>.Success(totalClasses, "Required classes counted.");

        #endregion
    }

    private async Task<Result<(List<SummaryDeveloper> Developers, int TotalCount)>> LoadSummaryPageAsync(
        int batchId,
        PaginationRequest request)
    {
        #region Load Summary Page

        var query = dbContext.BatchDevelopers
            .Where(bd => bd.BatchId == batchId)
            .Select(bd => new SummaryDeveloper
            {
                Id = bd.Developer.Id,
                FullName = bd.Developer.FullName,
                DeveloperCode = bd.Developer.DeveloperCode
            });

        var totalCount = await query.CountAsync();

        var developers = await query
            .OrderBy(bd => bd.FullName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return Result<(List<SummaryDeveloper> Developers, int TotalCount)>.Success(
            (developers, totalCount),
            "Summary developers loaded.");

        #endregion
    }

    private async Task<Result<List<AttendanceRecord>>> LoadSummaryAttendanceAsync(
        int batchId,
        List<SummaryDeveloper> developers)
    {
        #region Load Summary Attendance

        var developerIds = developers.Select(d => d.Id).ToList();

        var attendance = await dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.BatchId == batchId && developerIds.Contains(a.DeveloperId))
            .ToListAsync();

        return Result<List<AttendanceRecord>>.Success(attendance, "Summary attendance loaded.");

        #endregion
    }

    private sealed class SummaryDeveloper
    {
        public int Id { get; init; }

        public string FullName { get; init; } = string.Empty;

        public string? DeveloperCode { get; init; }
    }

    private static Result<List<AttendanceSummaryResponse>> BuildAttendanceSummary(
        List<SummaryDeveloper> developers,
        List<AttendanceRecord> attendance,
        int totalClasses,
        int? minAttendancePercent)
    {
        #region Calculate summary values

        var attendanceByDeveloper = attendance.ToLookup(record => record.DeveloperId);
        var result = developers
            .Select(developer => CreateAttendanceSummary(
                developer,
                attendanceByDeveloper[developer.Id],
                totalClasses,
                minAttendancePercent))
            .ToList();

        return Result<List<AttendanceSummaryResponse>>.Success(result, "Attendance summary prepared.");

        #endregion
    }
}
