using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    public async Task<Result<List<AttendanceSummaryResponse>>> GetFullAttendanceSummaryAsync(int batchId)
    {
        try
        {
            #region Count Report Classes

            var countReportClassesResult = await CountRequiredClassesAsync(batchId);

            if (countReportClassesResult.IsFailure)
            {
                logger.LogInformation(
                    "GetFullAttendanceSummaryAsync => CountReportClasses failed. Reason={Reason}, NextAction=return failure",
                    countReportClassesResult.Message);

                return Result<List<AttendanceSummaryResponse>>.Failure(countReportClassesResult.Message);
            }

            var totalClasses = countReportClassesResult.Data;

            logger.LogInformation(
                "GetFullAttendanceSummaryAsync => CountReportClasses succeeded. Reason={Reason}, TotalClasses={TotalClasses}, NextAction=get min attendance percent",
                countReportClassesResult.Message,
                totalClasses);

            #endregion

            #region Load minimum attendance percentage

            var minimumResult = await GetMinAttendancePercentAsync(batchId);

            if (minimumResult.IsFailure)
            {
                logger.LogInformation(
                    "GetFullAttendanceSummaryAsync => Minimum attendance lookup failed. Reason={Reason}, NextAction=return failure",
                    minimumResult.Message);

                return Result<List<AttendanceSummaryResponse>>.Failure(minimumResult.Message);
            }

            var minAttendancePercent = minimumResult.Data;

            logger.LogInformation(
                "GetFullAttendanceSummaryAsync => Minimum attendance loaded. Percent={Percent}, Reason={Reason}, NextAction=load report developers",
                minAttendancePercent,
                minimumResult.Message);

            #endregion

            #region Load Report Developers

            var loadReportDevelopersResult = await LoadReportDevelopersAsync(batchId);

            if (loadReportDevelopersResult.IsFailure)
            {
                logger.LogInformation(
                    "GetFullAttendanceSummaryAsync => LoadReportDevelopers failed. Reason={Reason}, NextAction=return failure",
                    loadReportDevelopersResult.Message);

                return Result<List<AttendanceSummaryResponse>>.Failure(loadReportDevelopersResult.Message);
            }

            var developers = loadReportDevelopersResult.Data!;

            logger.LogInformation(
                "GetFullAttendanceSummaryAsync => LoadReportDevelopers succeeded. Reason={Reason}, DeveloperCount={DeveloperCount}, NextAction=load report attendance",
                loadReportDevelopersResult.Message,
                developers.Count);

            #endregion

            #region Load Report Attendance

            var loadReportAttendanceResult = await LoadReportAttendanceAsync(batchId);

            if (loadReportAttendanceResult.IsFailure)
            {
                logger.LogInformation(
                    "GetFullAttendanceSummaryAsync => LoadReportAttendance failed. Reason={Reason}, NextAction=return failure",
                    loadReportAttendanceResult.Message);

                return Result<List<AttendanceSummaryResponse>>.Failure(loadReportAttendanceResult.Message);
            }

            var attendance = loadReportAttendanceResult.Data!;

            logger.LogInformation(
                "GetFullAttendanceSummaryAsync => LoadReportAttendance succeeded. Reason={Reason}, AttendanceCount={AttendanceCount}, NextAction=build attendance summary",
                loadReportAttendanceResult.Message,
                attendance.Count);

            #endregion

            #region Build Attendance Summary

            var buildAttendanceSummaryResult = BuildAttendanceSummary(developers, attendance, totalClasses, minAttendancePercent);

            if (buildAttendanceSummaryResult.IsFailure)
            {
                logger.LogInformation(
                    "GetFullAttendanceSummaryAsync => BuildAttendanceSummary failed. Reason={Reason}, NextAction=return failure",
                    buildAttendanceSummaryResult.Message);

                return Result<List<AttendanceSummaryResponse>>.Failure(buildAttendanceSummaryResult.Message);
            }

            var result = buildAttendanceSummaryResult.Data!;

            logger.LogInformation(
                "GetFullAttendanceSummaryAsync => BuildAttendanceSummary succeeded, DeveloperCount={DeveloperCount}, Reason={Reason}, NextAction=return successful outcome",
                result.Count,
                buildAttendanceSummaryResult.Message);

            #endregion

            return Result<List<AttendanceSummaryResponse>>.Success(result);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetFullAttendanceSummaryAsync => Operation failed, NextAction=return failure");

            return Result<List<AttendanceSummaryResponse>>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
