using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    public async Task<Result<BatchResponse>> GetBatchByIdAsync(int id)
    {
        try
        {
            #region Find Batch

            var findBatchResult = await FindBatchAsync(id);

            if (findBatchResult.IsFailure)
            {
                logger.LogInformation("GetBatchByIdAsync => FindBatch failed. Reason={Reason}, NextAction=return failure", findBatchResult.Message);

                return Result<BatchResponse>.Failure(findBatchResult.Message);
            }

            var batch = findBatchResult.Data!;

            logger.LogInformation(
                "GetBatchByIdAsync => FindBatch succeeded. Reason={Reason}, BatchId={BatchId}, NextAction=return successful outcome",
                findBatchResult.Message,
                batch.Id);

            #endregion

            return Result<BatchResponse>.Success(batch);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetBatchByIdAsync => Operation failed, NextAction=return failure");

            return Result<BatchResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
