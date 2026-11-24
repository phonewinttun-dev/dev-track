using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    private async Task<Result<TrainingCalendar>> LoadAttendanceCalendarAsync(BulkAttendanceRequest request)
    {
        #region Find the attendance calendar date

        var calendar = await dbContext.TrainingCalendars
            .FirstOrDefaultAsync(c => c.BatchId == request.BatchId && c.TrainingDate == request.TrainingDate);

        if (calendar == null)
        {
            return Result<TrainingCalendar>.Failure("Date not found in calendar.");
        }

        return Result<TrainingCalendar>.Success(calendar, "Calendar date found.");

        #endregion
    }

    private async Task<Result> PrepareAttendanceAsync(
        BulkAttendanceRequest request,
        HashSet<int> submittedIds)
    {
        #region Prepare Attendance

        var existing = await dbContext.AttendanceRecords
            .Where(a => a.BatchId == request.BatchId && a.TrainingDate == request.TrainingDate)
            .ToDictionaryAsync(a => a.DeveloperId);

        var removedRecords = existing.Values
            .Where(record => !submittedIds.Contains(record.DeveloperId));

        dbContext.AttendanceRecords.RemoveRange(removedRecords);

        foreach (var item in request.Markings)
        {
            if (existing.TryGetValue(item.DeveloperId, out var record))
            {
                record.Status = item.Status;
                record.Remark = item.Remark;
                continue;
            }

            dbContext.AttendanceRecords.Add(new AttendanceRecord
            {
                BatchId = request.BatchId,
                DeveloperId = item.DeveloperId,
                TrainingDate = request.TrainingDate,
                Status = item.Status,
                Remark = item.Remark,
                CreatedAt = DateTime.Now
            });
        }

        return Result.Success("Attendance changes prepared.");

        #endregion
    }

    private async Task<Result> SaveAttendanceAsync()
    {
        #region Save Attendance

        await dbContext.SaveChangesAsync();

        return Result.Success("Attendance saved.");

        #endregion
    }
}
