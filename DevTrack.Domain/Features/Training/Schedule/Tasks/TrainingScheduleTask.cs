using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    private async Task<Result<(List<TrainingCalendar> Calendar, int TotalCount)>> LoadSchedulePageAsync(
        int batchId,
        PaginationRequest request)
    {
        #region Load Schedule Page

        var query = dbContext.TrainingCalendars
            .AsNoTracking()
            .Where(c => c.BatchId == batchId);

        var totalCount = await query.CountAsync();

        var calendar = await query
            .OrderBy(c => c.TrainingDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return Result<(List<TrainingCalendar> Calendar, int TotalCount)>.Success(
            (calendar, totalCount),
            "Schedule page loaded.");

        #endregion
    }

    private async Task<Result<List<DateOnly>>> LoadMarkedDatesAsync(
        int batchId,
        List<TrainingCalendar> calendar)
    {
        #region Load Marked Dates

        var dates = calendar.Select(c => c.TrainingDate).ToList();

        var markedDates = await dbContext.AttendanceRecords
            .Where(a => a.BatchId == batchId && dates.Contains(a.TrainingDate))
            .Select(a => a.TrainingDate)
            .Distinct()
            .ToListAsync();

        return Result<List<DateOnly>>.Success(markedDates, "Marked dates loaded.");

        #endregion
    }

    private static Result<List<TrainingCalendarResponse>> BuildSchedule(
        List<TrainingCalendar> calendar,
        List<DateOnly> markedDates)
    {
        #region Build the response values

        var result = calendar
            .Select(c => new TrainingCalendarResponse
        {
            Id = c.Id,
            BatchId = c.BatchId,
            TrainingDate = c.TrainingDate,
            DayType = c.DayType,
            IsAttendanceRequired = c.IsAttendanceRequired ?? false,
            AssignmentTitle = c.AssignmentTitle,
            AssignmentDueDate = c.AssignmentDueDate,
            Remark = c.Remark,
            IsAttendanceMarked = markedDates.Contains(c.TrainingDate)
            })
            .ToList();

        return Result<List<TrainingCalendarResponse>>.Success(result, "Response values prepared.");

        #endregion
    }
}
