using DevTrack.Database.Entities;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    private Result<Batch> PrepareBatch(BatchRequest request)
    {
        #region Prepare Batch

        var batch = new Batch
        {
            BatchName = request.BatchName,
            MentorName = request.MentorName,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TrainingMonths = request.TrainingMonths,
            DaysPerWeek = request.DaysPerWeek,
            MinAttendancePercent = request.MinAttendancePercent,
            MonthlyLeaveAllowed = request.MonthlyLeaveAllowed,
            CreatedAt = DateTime.Now
        };

        return Result<Batch>.Success(batch, "Batch prepared.");

        #endregion
    }

    private Result PrepareCalendar(Batch batch, List<DayOfWeek> trainingDays)
    {
        #region Prepare Calendar

        for (var day = batch.StartDate.DayNumber; day <= batch.EndDate.DayNumber; day++)
        {
            var currentDate = DateOnly.FromDayNumber(day);
            var isClassDay = trainingDays.Contains(currentDate.DayOfWeek);
            var calendarEntry = new TrainingCalendar
            {
                TrainingDate = currentDate,
                DayType = isClassDay ? "Class Day" : "Holiday",
                Remark = isClassDay ? "Regular Training Session" : "Weekend/Holiday",
                IsAttendanceRequired = isClassDay
            };

            batch.TrainingCalendars.Add(calendarEntry);
        }

        return Result.Success("Training calendar prepared.");

        #endregion
    }
}
