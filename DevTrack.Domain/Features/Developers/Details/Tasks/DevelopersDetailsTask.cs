using DevTrack.Features.TrainingManagement.Training;
using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Developers;

public partial class DeveloperService
{
    private async Task<Result<DeveloperDetailResponse>> FindDeveloperAsync(int id)
    {
        #region Find Developer

        var developer = await dbContext.Developers
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DeveloperDetailResponse
            {
                Id = d.Id,
                FullName = d.FullName,
                DeveloperCode = d.DeveloperCode,
                Email = d.Email,
                Phone = d.Phone,
                IsActive = d.IsActive ?? true,
                CreatedAt = d.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (developer == null)
        {
            return Result<DeveloperDetailResponse>.Failure("Developer not found.");
        }

        return Result<DeveloperDetailResponse>.Success(developer, "Developer found.");

        #endregion
    }

    private async Task<Result<DeveloperBatchSummary?>> FindCurrentBatchAsync(int id)
    {
        #region Find Current Batch

        var currentBatch = await dbContext.BatchDevelopers
            .AsNoTracking()
            .Where(bd => bd.DeveloperId == id)
            .OrderByDescending(bd => bd.Batch.StartDate)
            .Select(bd => new DeveloperBatchSummary
            {
                BatchId = bd.Batch.Id,
                BatchName = bd.Batch.BatchName,
                MentorName = bd.Batch.MentorName,
                StartDate = bd.Batch.StartDate,
                EndDate = bd.Batch.EndDate,
                MinAttendancePercent = bd.Batch.MinAttendancePercent
            })
            .FirstOrDefaultAsync();

        return Result<DeveloperBatchSummary?>.Success(currentBatch, "Current batch lookup completed.");

        #endregion
    }

    private async Task<Result<int>> CountDeveloperClassesAsync(int batchId)
    {
        #region Count Developer Classes

        var totalClasses = await dbContext.TrainingCalendars
            .AsNoTracking()
            .CountAsync(c => c.BatchId == batchId && (c.IsAttendanceRequired ?? false));

        return Result<int>.Success(totalClasses, "Required classes counted.");

        #endregion
    }

    private async Task<Result<List<AttendanceRecord>>> LoadDeveloperAttendanceAsync(int batchId, int id)
    {
        #region Load Developer Attendance

        var attendance = await dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.BatchId == batchId && a.DeveloperId == id)
            .ToListAsync();

        return Result<List<AttendanceRecord>>.Success(attendance, "Developer attendance loaded.");

        #endregion
    }

    private static Result<DeveloperDetailResponse> BuildDeveloperDetails(
        DeveloperDetailResponse developer,
        DeveloperBatchSummary currentBatch,
        List<AttendanceRecord> attendance,
        int totalClasses)
    {
        #region Build the response values

        var metrics = AttendanceMetrics.Calculate(attendance, totalClasses, currentBatch.MinAttendancePercent);

        developer.Attendance = new DeveloperAttendanceSummary
        {
            TotalClasses = totalClasses,
            PresentDays = metrics.PresentDays,
            AbsentDays = metrics.AbsentDays,
            LeaveDays = metrics.LeaveDays,
            LateDays = metrics.LateDays,
            AttendancePercent = metrics.AttendancePercent,
            Status = metrics.Status
        };

        developer.RecentAttendance = attendance
            .OrderByDescending(a => a.TrainingDate)
            .Take(10)
            .Select(a => new DeveloperAttendanceRecordResponse
            {
                TrainingDate = a.TrainingDate,
                Status = a.Status,
                Remark = a.Remark
            })
            .ToList();

        return Result<DeveloperDetailResponse>.Success(developer, "Response values prepared.");

        #endregion
    }
}
