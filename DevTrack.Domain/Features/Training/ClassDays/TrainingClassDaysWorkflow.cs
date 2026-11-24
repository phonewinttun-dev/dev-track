using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    public async Task<Result<List<DateOnly>>> GetClassDaysAsync(int batchId)
    {
        try
        {
            #region Load Class Days

            var loadClassDaysResult = await LoadClassDaysAsync(batchId);

            if (loadClassDaysResult.IsFailure)
            {
                logger.LogInformation(
                    "GetClassDaysAsync => LoadClassDays failed. Reason={Reason}, NextAction=return failure",
                    loadClassDaysResult.Message);

                return Result<List<DateOnly>>.Failure(loadClassDaysResult.Message);
            }

            var dates = loadClassDaysResult.Data!;

            logger.LogInformation(
                "GetClassDaysAsync => LoadClassDays succeeded. Reason={Reason}, DateCount={DateCount}, NextAction=return successful outcome",
                loadClassDaysResult.Message,
                dates.Count);

            #endregion

            return Result<List<DateOnly>>.Success(dates);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetClassDaysAsync => Operation failed, NextAction=return failure");

            return Result<List<DateOnly>>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
