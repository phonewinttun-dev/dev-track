using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    public async Task<PagedResult<BatchResponse>> GetBatchesAsync(PaginationRequest request)
    {
        try
        {
            #region Load Batch Page

            var loadBatchPageResult = await LoadBatchPageAsync(request);

            if (loadBatchPageResult.IsFailure)
            {
                logger.LogInformation(
                    "GetBatchesAsync => LoadBatchPage failed. Reason={Reason}, NextAction=return failure",
                    loadBatchPageResult.Message);

                return PagedResult<BatchResponse>.Failure(loadBatchPageResult.Message);
            }

            var (batches, totalCount) = loadBatchPageResult.Data;

            logger.LogInformation(
                "GetBatchesAsync => LoadBatchPage succeeded. Reason={Reason}, BatchCount={BatchCount}, NextAction=return successful outcome",
                loadBatchPageResult.Message,
                batches.Count);

            #endregion

            var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
            return PagedResult<BatchResponse>.Success(batches, pagination);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetBatchesAsync => Operation failed, NextAction=return failure");

            return PagedResult<BatchResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
