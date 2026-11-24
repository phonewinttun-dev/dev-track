using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    public async Task<Result<BulkAttendanceRequest>> GetAttendanceForDateAsync(int batchId, DateOnly date)
    {
        try
        {
            #region Find Attendance Date

            var findAttendanceDateResult = await FindAttendanceDateAsync(batchId, date);

            if (findAttendanceDateResult.IsFailure)
            {
                logger.LogInformation(
                    "GetAttendanceForDateAsync => FindAttendanceDate failed. Reason={Reason}, NextAction=return failure",
                    findAttendanceDateResult.Message);

                return Result<BulkAttendanceRequest>.Failure(findAttendanceDateResult.Message);
            }

            var calendar = findAttendanceDateResult.Data!;

            logger.LogInformation(
                "GetAttendanceForDateAsync => FindAttendanceDate succeeded. Reason={Reason}, Date={Date}, NextAction=load attendance roster",
                findAttendanceDateResult.Message,
                date);

            #endregion

            #region Load Attendance Roster

            var loadAttendanceRosterResult = await LoadAttendanceRosterAsync(batchId);

            if (loadAttendanceRosterResult.IsFailure)
            {
                logger.LogInformation(
                    "GetAttendanceForDateAsync => LoadAttendanceRoster failed. Reason={Reason}, NextAction=return failure",
                    loadAttendanceRosterResult.Message);

                return Result<BulkAttendanceRequest>.Failure(loadAttendanceRosterResult.Message);
            }

            var batchDevelopers = loadAttendanceRosterResult.Data!;

            logger.LogInformation(
                "GetAttendanceForDateAsync => LoadAttendanceRoster succeeded. Reason={Reason}, DeveloperCount={DeveloperCount}, NextAction=load date attendance",
                loadAttendanceRosterResult.Message,
                batchDevelopers.Count);

            #endregion

            #region Load Date Attendance

            var loadDateAttendanceResult = await LoadDateAttendanceAsync(batchId, date);

            if (loadDateAttendanceResult.IsFailure)
            {
                logger.LogInformation(
                    "GetAttendanceForDateAsync => LoadDateAttendance failed. Reason={Reason}, NextAction=return failure",
                    loadDateAttendanceResult.Message);

                return Result<BulkAttendanceRequest>.Failure(loadDateAttendanceResult.Message);
            }

            var existingAttendance = loadDateAttendanceResult.Data!;

            logger.LogInformation(
                "GetAttendanceForDateAsync => LoadDateAttendance succeeded. Reason={Reason}, AttendanceCount={AttendanceCount}, NextAction=build attendance form",
                loadDateAttendanceResult.Message,
                existingAttendance.Count);

            #endregion

            #region Build Attendance Form

            var buildAttendanceFormResult = BuildAttendanceForm(batchId, date, batchDevelopers, existingAttendance);

            if (buildAttendanceFormResult.IsFailure)
            {
                logger.LogInformation(
                    "GetAttendanceForDateAsync => BuildAttendanceForm failed. Reason={Reason}, NextAction=return failure",
                    buildAttendanceFormResult.Message);

                return Result<BulkAttendanceRequest>.Failure(buildAttendanceFormResult.Message);
            }

            var request = buildAttendanceFormResult.Data!;

            logger.LogInformation(
                "GetAttendanceForDateAsync => BuildAttendanceForm succeeded, DeveloperCount={DeveloperCount}, Reason={Reason}, NextAction=return successful outcome",
                request.Markings.Count,
                buildAttendanceFormResult.Message);

            #endregion

            return Result<BulkAttendanceRequest>.Success(request);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetAttendanceForDateAsync => Operation failed, NextAction=return failure");

            return Result<BulkAttendanceRequest>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
