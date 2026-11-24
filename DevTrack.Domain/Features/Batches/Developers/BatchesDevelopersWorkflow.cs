using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    public async Task<Result<List<BatchAssignmentModel>>> GetBatchDevelopersAsync(int batchId)
    {
        try
        {
            #region Load Assigned Developers

            var loadAssignedDevelopersResult = await LoadAssignedDevelopersAsync(batchId);

            if (loadAssignedDevelopersResult.IsFailure)
            {
                logger.LogInformation(
                    "GetBatchDevelopersAsync => LoadAssignedDevelopers failed. Reason={Reason}, NextAction=return failure",
                    loadAssignedDevelopersResult.Message);

                return Result<List<BatchAssignmentModel>>.Failure(loadAssignedDevelopersResult.Message);
            }

            var assigned = loadAssignedDevelopersResult.Data!;

            logger.LogInformation(
                "GetBatchDevelopersAsync => LoadAssignedDevelopers succeeded. Reason={Reason}, AssignedCount={AssignedCount}, NextAction=load available developers",
                loadAssignedDevelopersResult.Message,
                assigned.Count);

            #endregion

            #region Load Available Developers

            var loadAvailableDevelopersResult = await LoadAvailableDevelopersAsync(assigned);

            if (loadAvailableDevelopersResult.IsFailure)
            {
                logger.LogInformation(
                    "GetBatchDevelopersAsync => LoadAvailableDevelopers failed. Reason={Reason}, NextAction=return failure",
                    loadAvailableDevelopersResult.Message);

                return Result<List<BatchAssignmentModel>>.Failure(loadAvailableDevelopersResult.Message);
            }

            var available = loadAvailableDevelopersResult.Data!;

            logger.LogInformation(
                "GetBatchDevelopersAsync => LoadAvailableDevelopers succeeded. Reason={Reason}, AvailableCount={AvailableCount}, NextAction=return successful outcome",
                loadAvailableDevelopersResult.Message,
                available.Count);

            #endregion

            var result = assigned.Concat(available).OrderBy(x => x.FullName).ToList();
            return Result<List<BatchAssignmentModel>>.Success(result);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetBatchDevelopersAsync => Operation failed, NextAction=return failure");

            return Result<List<BatchAssignmentModel>>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
