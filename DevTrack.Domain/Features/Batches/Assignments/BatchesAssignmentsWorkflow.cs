using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    public async Task<Result> UpdateBatchAssignmentsAsync(int batchId, List<int> selectedDeveloperIds)
    {
        try
        {
            #region Prepare Assignments

            var prepareAssignmentsResult = await PrepareAssignmentsAsync(batchId, selectedDeveloperIds);

            if (prepareAssignmentsResult.IsFailure)
            {
                logger.LogInformation(
                    "UpdateBatchAssignmentsAsync => PrepareAssignments failed. Reason={Reason}, NextAction=return failure",
                    prepareAssignmentsResult.Message);

                return Result.Failure(prepareAssignmentsResult.Message);
            }

            logger.LogInformation(
                "UpdateBatchAssignmentsAsync => PrepareAssignments succeeded. Reason={Reason}, DeveloperCount={DeveloperCount}, NextAction=save assignments",
                prepareAssignmentsResult.Message,
                selectedDeveloperIds.Count);

            #endregion

            #region Save Assignments

            var saveAssignmentsResult = await SaveAssignmentsAsync();

            if (saveAssignmentsResult.IsFailure)
            {
                logger.LogInformation(
                    "UpdateBatchAssignmentsAsync => SaveAssignments failed. Reason={Reason}, NextAction=return failure",
                    saveAssignmentsResult.Message);

                return Result.Failure(saveAssignmentsResult.Message);
            }

            logger.LogInformation(
                "UpdateBatchAssignmentsAsync => SaveAssignments succeeded. Reason={Reason}, NextAction=return successful outcome",
                saveAssignmentsResult.Message);

            #endregion

            return Result.Success("Assignments updated successfully.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "UpdateBatchAssignmentsAsync => Operation failed, NextAction=return failure");

            return Result.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
