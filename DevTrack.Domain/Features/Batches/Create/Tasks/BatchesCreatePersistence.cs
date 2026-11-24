using DevTrack.Database.Entities;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    private async Task<Result> SaveBatchAsync(Batch batch)
    {
        #region Save Batch

        dbContext.Batches.Add(batch);
        await dbContext.SaveChangesAsync();

        return Result.Success("Batch saved.");

        #endregion
    }
}
