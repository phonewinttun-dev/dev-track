using Shared;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    private static Result ValidateBatch(BatchRequest request)
    {
        #region Validate Batch

        if (request.BatchName.IsNullOrEmpty())
        {
            return Result.Failure("Batch name is required.");
        }

        return Result.Success("Batch request is valid.");

        #endregion
    }
}
