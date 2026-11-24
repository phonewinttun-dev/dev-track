using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    private async Task<Result> PrepareAssignmentsAsync(int batchId, List<int> selectedDeveloperIds)
    {
        #region Prepare Assignments

        var existing = await dbContext.BatchDevelopers
            .Where(bd => bd.BatchId == batchId)
            .ToListAsync();
        dbContext.BatchDevelopers.RemoveRange(existing);

        foreach (var developerId in selectedDeveloperIds)
        {
            dbContext.BatchDevelopers.Add(new BatchDeveloper
            {
                BatchId = batchId,
                DeveloperId = developerId
            });
        }

        return Result.Success("Batch assignments prepared.");

        #endregion
    }

    private async Task<Result> SaveAssignmentsAsync()
    {
        #region Save Assignments

        await dbContext.SaveChangesAsync();

        return Result.Success("Batch assignments saved.");

        #endregion
    }
}
