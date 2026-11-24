using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Developers;

public partial class DeveloperService
{
    public async Task<Result<DeveloperDetailResponse>> GetDeveloperByIdAsync(int id)
    {
        try
        {
            #region Find Developer

            var findDeveloperResult = await FindDeveloperAsync(id);

            if (findDeveloperResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDeveloperByIdAsync => FindDeveloper failed. Reason={Reason}, NextAction=return failure",
                    findDeveloperResult.Message);

                return Result<DeveloperDetailResponse>.Failure(findDeveloperResult.Message);
            }

            var developer = findDeveloperResult.Data!;

            logger.LogInformation(
                "GetDeveloperByIdAsync => FindDeveloper succeeded. Reason={Reason}, DeveloperId={DeveloperId}, NextAction=find current batch",
                findDeveloperResult.Message,
                developer.Id);

            #endregion

            #region Find Current Batch

            var findCurrentBatchResult = await FindCurrentBatchAsync(id);

            if (findCurrentBatchResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDeveloperByIdAsync => FindCurrentBatch failed. Reason={Reason}, NextAction=return failure",
                    findCurrentBatchResult.Message);

                return Result<DeveloperDetailResponse>.Failure(findCurrentBatchResult.Message);
            }

            var currentBatch = findCurrentBatchResult.Data;

            logger.LogInformation(
                "GetDeveloperByIdAsync => FindCurrentBatch succeeded. Reason={Reason}, BatchId={BatchId}, NextAction=check whether a batch is assigned",
                findCurrentBatchResult.Message,
                currentBatch?.BatchId);

            #endregion

            developer.CurrentBatch = currentBatch;

            if (currentBatch == null)
            {
                logger.LogInformation(
                    "GetDeveloperByIdAsync => No batch is assigned. DeveloperId={DeveloperId}, NextAction=return developer without a batch",
                    developer.Id);

                return Result<DeveloperDetailResponse>.Success(developer);
            }

            #region Count Developer Classes

            var countDeveloperClassesResult = await CountDeveloperClassesAsync(currentBatch.BatchId);

            if (countDeveloperClassesResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDeveloperByIdAsync => CountDeveloperClasses failed. Reason={Reason}, NextAction=return failure",
                    countDeveloperClassesResult.Message);

                return Result<DeveloperDetailResponse>.Failure(countDeveloperClassesResult.Message);
            }

            var totalClasses = countDeveloperClassesResult.Data;

            logger.LogInformation(
                "GetDeveloperByIdAsync => CountDeveloperClasses succeeded. Reason={Reason}, TotalClasses={TotalClasses}, NextAction=load developer attendance",
                countDeveloperClassesResult.Message,
                totalClasses);

            #endregion

            #region Load Developer Attendance

            var loadDeveloperAttendanceResult = await LoadDeveloperAttendanceAsync(currentBatch.BatchId, id);

            if (loadDeveloperAttendanceResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDeveloperByIdAsync => LoadDeveloperAttendance failed. Reason={Reason}, NextAction=return failure",
                    loadDeveloperAttendanceResult.Message);

                return Result<DeveloperDetailResponse>.Failure(loadDeveloperAttendanceResult.Message);
            }

            var attendance = loadDeveloperAttendanceResult.Data!;

            logger.LogInformation(
                "GetDeveloperByIdAsync => LoadDeveloperAttendance succeeded. Reason={Reason}, AttendanceCount={AttendanceCount}, NextAction=build developer details",
                loadDeveloperAttendanceResult.Message,
                attendance.Count);

            #endregion

            #region Build Developer Details

            var buildDeveloperDetailsResult = BuildDeveloperDetails(developer, currentBatch, attendance, totalClasses);

            if (buildDeveloperDetailsResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDeveloperByIdAsync => BuildDeveloperDetails failed. Reason={Reason}, NextAction=return failure",
                    buildDeveloperDetailsResult.Message);

                return Result<DeveloperDetailResponse>.Failure(buildDeveloperDetailsResult.Message);
            }

            var details = buildDeveloperDetailsResult.Data!;

            logger.LogInformation(
                "GetDeveloperByIdAsync => BuildDeveloperDetails succeeded, DeveloperId={DeveloperId}, Reason={Reason}, NextAction=return successful outcome",
                details.Id,
                buildDeveloperDetailsResult.Message);

            #endregion

            return Result<DeveloperDetailResponse>.Success(details);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetDeveloperByIdAsync => Operation failed, NextAction=return failure");

            return Result<DeveloperDetailResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
