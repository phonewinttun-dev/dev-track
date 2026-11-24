using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    public async Task<PagedResult<TrainingCalendarResponse>> GetScheduleAsync(
        int batchId,
        PaginationRequest request)
    {
        try
        {
            #region Load Schedule Page

            var loadSchedulePageResult = await LoadSchedulePageAsync(batchId, request);

            if (loadSchedulePageResult.IsFailure)
            {
                logger.LogInformation(
                    "GetScheduleAsync => LoadSchedulePage failed. Reason={Reason}, NextAction=return failure",
                    loadSchedulePageResult.Message);

                return PagedResult<TrainingCalendarResponse>.Failure(loadSchedulePageResult.Message);
            }

            var (calendar, totalCount) = loadSchedulePageResult.Data;

            logger.LogInformation(
                "GetScheduleAsync => LoadSchedulePage succeeded. Reason={Reason}, CalendarCount={CalendarCount}, NextAction=load marked dates",
                loadSchedulePageResult.Message,
                calendar.Count);

            #endregion

            #region Load Marked Dates

            var loadMarkedDatesResult = await LoadMarkedDatesAsync(batchId, calendar);

            if (loadMarkedDatesResult.IsFailure)
            {
                logger.LogInformation(
                    "GetScheduleAsync => LoadMarkedDates failed. Reason={Reason}, NextAction=return failure",
                    loadMarkedDatesResult.Message);

                return PagedResult<TrainingCalendarResponse>.Failure(loadMarkedDatesResult.Message);
            }

            var markedDates = loadMarkedDatesResult.Data!;

            logger.LogInformation(
                "GetScheduleAsync => LoadMarkedDates succeeded. Reason={Reason}, MarkedDateCount={MarkedDateCount}, NextAction=build schedule",
                loadMarkedDatesResult.Message,
                markedDates.Count);

            #endregion

            #region Build Schedule

            var buildScheduleResult = BuildSchedule(calendar, markedDates);

            if (buildScheduleResult.IsFailure)
            {
                logger.LogInformation(
                    "GetScheduleAsync => BuildSchedule failed. Reason={Reason}, NextAction=return failure",
                    buildScheduleResult.Message);

                return PagedResult<TrainingCalendarResponse>.Failure(buildScheduleResult.Message);
            }

            var result = buildScheduleResult.Data!;

            logger.LogInformation(
                "GetScheduleAsync => BuildSchedule succeeded, EntryCount={EntryCount}, Reason={Reason}, NextAction=return successful outcome",
                result.Count,
                buildScheduleResult.Message);

            #endregion

            var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
            return PagedResult<TrainingCalendarResponse>.Success(result, pagination);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetScheduleAsync => Operation failed, NextAction=return failure");

            return PagedResult<TrainingCalendarResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
