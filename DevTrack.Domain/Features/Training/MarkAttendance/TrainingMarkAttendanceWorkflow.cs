using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    public async Task<Result> MarkBulkAttendanceAsync(BulkAttendanceRequest request)
    {
        try
        {
            #region Find the attendance calendar date

            var calendarResult = await LoadAttendanceCalendarAsync(request);

            if (calendarResult.IsFailure)
            {
                logger.LogInformation(
                    "MarkBulkAttendanceAsync => Calendar lookup failed. BatchId={BatchId}, Date={Date}, Reason={Reason}, NextAction=return failure",
                    request.BatchId,
                    request.TrainingDate,
                    calendarResult.Message);

                return Result.Failure(calendarResult.Message);
            }

            var calendar = calendarResult.Data!;

            logger.LogInformation(
                "MarkBulkAttendanceAsync => Calendar date found. BatchId={BatchId}, Date={Date}, Reason={Reason}, NextAction=check class-day eligibility",
                request.BatchId,
                calendar.TrainingDate,
                calendarResult.Message);

            #endregion

            #region Check class-day eligibility

            var attendanceDayResult = CheckAttendanceDay(calendar);

            if (attendanceDayResult.IsFailure)
            {
                logger.LogInformation(
                    "MarkBulkAttendanceAsync => Class-day check failed. Date={Date}, Reason={Reason}, NextAction=return failure",
                    calendar.TrainingDate,
                    attendanceDayResult.Message);

                return Result.Failure(attendanceDayResult.Message);
            }

            logger.LogInformation(
                "MarkBulkAttendanceAsync => Class-day check passed. Date={Date}, Reason={Reason}, NextAction=validate markings",
                calendar.TrainingDate,
                attendanceDayResult.Message);

            #endregion

            #region Validate Markings

            var validateMarkingsResult = ValidateMarkings(request);

            if (validateMarkingsResult.IsFailure)
            {
                logger.LogInformation(
                    "MarkBulkAttendanceAsync => ValidateMarkings failed. Reason={Reason}, NextAction=return failure",
                    validateMarkingsResult.Message);

                return Result.Failure(validateMarkingsResult.Message);
            }

            logger.LogInformation(
                "MarkBulkAttendanceAsync => ValidateMarkings succeeded. Reason={Reason}, NextAction=check roster",
                validateMarkingsResult.Message);

            #endregion

            #region Check Roster

            var rosterResult = await CheckRosterAsync(request);

            if (rosterResult.IsFailure)
            {
                logger.LogInformation(
                    "MarkBulkAttendanceAsync => CheckRoster failed. Reason={Reason}, NextAction=return failure",
                    rosterResult.Message);

                return Result.Failure(rosterResult.Message);
            }

            var submittedIds = rosterResult.Data!;

            logger.LogInformation(
                "MarkBulkAttendanceAsync => CheckRoster succeeded. Reason={Reason}, MarkingCount={MarkingCount}, NextAction=prepare attendance",
                rosterResult.Message,
                submittedIds.Count);

            #endregion

            #region Prepare Attendance

            var prepareAttendanceResult = await PrepareAttendanceAsync(request, submittedIds);

            if (prepareAttendanceResult.IsFailure)
            {
                logger.LogInformation(
                    "MarkBulkAttendanceAsync => PrepareAttendance failed. Reason={Reason}, NextAction=return failure",
                    prepareAttendanceResult.Message);

                return Result.Failure(prepareAttendanceResult.Message);
            }

            logger.LogInformation(
                "MarkBulkAttendanceAsync => PrepareAttendance succeeded. Reason={Reason}, MarkingCount={MarkingCount}, NextAction=save attendance",
                prepareAttendanceResult.Message,
                submittedIds.Count);

            #endregion

            #region Save Attendance

            var saveAttendanceResult = await SaveAttendanceAsync();

            if (saveAttendanceResult.IsFailure)
            {
                logger.LogInformation(
                    "MarkBulkAttendanceAsync => SaveAttendance failed. Reason={Reason}, NextAction=return failure",
                    saveAttendanceResult.Message);

                return Result.Failure(saveAttendanceResult.Message);
            }

            logger.LogInformation(
                "MarkBulkAttendanceAsync => SaveAttendance succeeded. Reason={Reason}, NextAction=return successful outcome",
                saveAttendanceResult.Message);

            #endregion

            return Result.Success("Attendance marked successfully.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "MarkBulkAttendanceAsync => Operation failed, NextAction=return failure");

            return Result.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
