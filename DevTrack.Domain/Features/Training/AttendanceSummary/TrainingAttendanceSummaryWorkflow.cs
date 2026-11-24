using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    public async Task<PagedResult<AttendanceSummaryResponse>> GetAttendanceSummaryAsync(
        int batchId,
        PaginationRequest request)
    {
        try
        {
            #region Count Summary Classes

            var countSummaryClassesResult = await CountRequiredClassesAsync(batchId);

            if (countSummaryClassesResult.IsFailure)
            {
                logger.LogInformation(
                    "GetAttendanceSummaryAsync => CountSummaryClasses failed. Reason={Reason}, NextAction=return failure",
                    countSummaryClassesResult.Message);

                return PagedResult<AttendanceSummaryResponse>.Failure(countSummaryClassesResult.Message);
            }

            var totalClasses = countSummaryClassesResult.Data;

            logger.LogInformation(
                "GetAttendanceSummaryAsync => CountSummaryClasses succeeded. Reason={Reason}, TotalClasses={TotalClasses}, NextAction=get min attendance percent",
                countSummaryClassesResult.Message,
                totalClasses);

            #endregion

            #region Load minimum attendance percentage

            var minimumResult = await GetMinAttendancePercentAsync(batchId);

            if (minimumResult.IsFailure)
            {
                logger.LogInformation(
                    "GetAttendanceSummaryAsync => Minimum attendance lookup failed. Reason={Reason}, NextAction=return failure",
                    minimumResult.Message);

                return PagedResult<AttendanceSummaryResponse>.Failure(minimumResult.Message);
            }

            var minAttendancePercent = minimumResult.Data;

            logger.LogInformation(
                "GetAttendanceSummaryAsync => Minimum attendance loaded. Percent={Percent}, Reason={Reason}, NextAction=load summary page",
                minAttendancePercent,
                minimumResult.Message);

            #endregion

            #region Load Summary Page

            var loadSummaryPageResult = await LoadSummaryPageAsync(batchId, request);

            if (loadSummaryPageResult.IsFailure)
            {
                logger.LogInformation(
                    "GetAttendanceSummaryAsync => LoadSummaryPage failed. Reason={Reason}, NextAction=return failure",
                    loadSummaryPageResult.Message);

                return PagedResult<AttendanceSummaryResponse>.Failure(loadSummaryPageResult.Message);
            }

            var (developers, totalCount) = loadSummaryPageResult.Data;

            logger.LogInformation(
                "GetAttendanceSummaryAsync => LoadSummaryPage succeeded. Reason={Reason}, DeveloperCount={DeveloperCount}, NextAction=load summary attendance",
                loadSummaryPageResult.Message,
                developers.Count);

            #endregion

            #region Load Summary Attendance

            var loadSummaryAttendanceResult = await LoadSummaryAttendanceAsync(batchId, developers);

            if (loadSummaryAttendanceResult.IsFailure)
            {
                logger.LogInformation(
                    "GetAttendanceSummaryAsync => LoadSummaryAttendance failed. Reason={Reason}, NextAction=return failure",
                    loadSummaryAttendanceResult.Message);

                return PagedResult<AttendanceSummaryResponse>.Failure(loadSummaryAttendanceResult.Message);
            }

            var attendance = loadSummaryAttendanceResult.Data!;

            logger.LogInformation(
                "GetAttendanceSummaryAsync => LoadSummaryAttendance succeeded. Reason={Reason}, AttendanceCount={AttendanceCount}, NextAction=build attendance summary",
                loadSummaryAttendanceResult.Message,
                attendance.Count);

            #endregion

            #region Build Attendance Summary

            var buildAttendanceSummaryResult = BuildAttendanceSummary(developers, attendance, totalClasses, minAttendancePercent);

            if (buildAttendanceSummaryResult.IsFailure)
            {
                logger.LogInformation(
                    "GetAttendanceSummaryAsync => BuildAttendanceSummary failed. Reason={Reason}, NextAction=return failure",
                    buildAttendanceSummaryResult.Message);

                return PagedResult<AttendanceSummaryResponse>.Failure(buildAttendanceSummaryResult.Message);
            }

            var result = buildAttendanceSummaryResult.Data!;

            logger.LogInformation(
                "GetAttendanceSummaryAsync => BuildAttendanceSummary succeeded, DeveloperCount={DeveloperCount}, Reason={Reason}, NextAction=return successful outcome",
                result.Count,
                buildAttendanceSummaryResult.Message);

            #endregion

            var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
            return PagedResult<AttendanceSummaryResponse>.Success(result, pagination);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetAttendanceSummaryAsync => Operation failed, NextAction=return failure");

            return PagedResult<AttendanceSummaryResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
