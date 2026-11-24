using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    public async Task<Result<BatchResponse>> CreateBatchAsync(BatchRequest request)
    {
        try
        {
            #region Validate Batch

            var validateBatchResult = ValidateBatch(request);

            if (validateBatchResult.IsFailure)
            {
                logger.LogInformation(
                    "CreateBatchAsync => ValidateBatch failed. Reason={Reason}, NextAction=return failure",
                    validateBatchResult.Message);

                return Result<BatchResponse>.Failure(validateBatchResult.Message);
            }

            logger.LogInformation(
                "CreateBatchAsync => ValidateBatch succeeded. Reason={Reason}, NextAction=prepare batch",
                validateBatchResult.Message);

            #endregion

            #region Prepare Batch

            var prepareBatchResult = PrepareBatch(request);

            if (prepareBatchResult.IsFailure)
            {
                logger.LogInformation("CreateBatchAsync => PrepareBatch failed. Reason={Reason}, NextAction=return failure", prepareBatchResult.Message);

                return Result<BatchResponse>.Failure(prepareBatchResult.Message);
            }

            var batch = prepareBatchResult.Data!;

            logger.LogInformation(
                "CreateBatchAsync => PrepareBatch succeeded. Reason={Reason}, NextAction=prepare calendar",
                prepareBatchResult.Message);

            #endregion

            #region Prepare Calendar

            var prepareCalendarResult = PrepareCalendar(batch, request.SelectedDays);

            if (prepareCalendarResult.IsFailure)
            {
                logger.LogInformation(
                    "CreateBatchAsync => PrepareCalendar failed. Reason={Reason}, NextAction=return failure",
                    prepareCalendarResult.Message);

                return Result<BatchResponse>.Failure(prepareCalendarResult.Message);
            }

            logger.LogInformation(
                "CreateBatchAsync => PrepareCalendar succeeded. Reason={Reason}, NextAction=save batch",
                prepareCalendarResult.Message);

            #endregion

            #region Save Batch

            var saveBatchResult = await SaveBatchAsync(batch);

            if (saveBatchResult.IsFailure)
            {
                logger.LogInformation("CreateBatchAsync => SaveBatch failed. Reason={Reason}, NextAction=return failure", saveBatchResult.Message);

                return Result<BatchResponse>.Failure(saveBatchResult.Message);
            }

            logger.LogInformation(
                "CreateBatchAsync => SaveBatch succeeded. Reason={Reason}, BatchId={BatchId}, NextAction=return successful outcome",
                saveBatchResult.Message,
                batch.Id);

            #endregion

            return Result<BatchResponse>.Success(new BatchResponse
            {
                Id = batch.Id,
                BatchName = batch.BatchName,
                MentorName = batch.MentorName,
                StartDate = batch.StartDate,
                EndDate = batch.EndDate,
                TrainingMonths = batch.TrainingMonths
            }, "Batch created successfully.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "CreateBatchAsync => Operation failed, NextAction=return failure");

            return Result<BatchResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
