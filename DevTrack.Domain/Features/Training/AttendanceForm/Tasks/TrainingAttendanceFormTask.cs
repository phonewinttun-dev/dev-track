using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    private async Task<Result<TrainingCalendar>> FindAttendanceDateAsync(int batchId, DateOnly date)
    {
        #region Find Attendance Date

        var calendar = await dbContext.TrainingCalendars
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.BatchId == batchId && c.TrainingDate == date);

        if (calendar == null)
        {
            return Result<TrainingCalendar>.Failure("Date not found in calendar.");
        }

        return Result<TrainingCalendar>.Success(calendar, "Calendar date found.");

        #endregion
    }

    private async Task<Result<List<Developer>>> LoadAttendanceRosterAsync(int batchId)
    {
        #region Load Attendance Roster

        var batchDevelopers = await dbContext.BatchDevelopers
            .AsNoTracking()
            .Where(bd => bd.BatchId == batchId)
            .Select(bd => bd.Developer)
            .ToListAsync();

        return Result<List<Developer>>.Success(batchDevelopers, "Attendance roster loaded.");

        #endregion
    }

    private async Task<Result<Dictionary<int, AttendanceRecord>>> LoadDateAttendanceAsync(
        int batchId,
        DateOnly date)
    {
        #region Load Date Attendance

        var existingAttendance = await dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.BatchId == batchId && a.TrainingDate == date)
            .ToDictionaryAsync(a => a.DeveloperId);

        return Result<Dictionary<int, AttendanceRecord>>.Success(existingAttendance, "Existing attendance loaded.");

        #endregion
    }

    private static Result<BulkAttendanceRequest> BuildAttendanceForm(
        int batchId,
        DateOnly date,
        List<Developer> batchDevelopers,
        Dictionary<int, AttendanceRecord> existingAttendance)
    {
        #region Build the response values

        var request = new BulkAttendanceRequest
        {
            BatchId = batchId,
            TrainingDate = date,
            Markings = batchDevelopers
                .Select(d => new AttendanceMarkingModel
                {
                    DeveloperId = d.Id,
                    FullName = d.FullName,
                    Status = existingAttendance.TryGetValue(d.Id, out var marking) ? marking.Status : "Present",
                    Remark = marking is not null ? marking.Remark : ""
                })
                .OrderBy(m => m.FullName)
                .ToList()
        };

        return Result<BulkAttendanceRequest>.Success(request, "Response values prepared.");

        #endregion
    }
}
