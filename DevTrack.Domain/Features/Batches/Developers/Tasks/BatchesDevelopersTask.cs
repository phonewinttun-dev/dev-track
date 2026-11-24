using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    private async Task<Result<List<BatchAssignmentModel>>> LoadAssignedDevelopersAsync(int batchId)
    {
        #region Load Assigned Developers

        var assigned = await dbContext.BatchDevelopers
            .Where(bd => bd.BatchId == batchId)
            .Select(bd => new BatchAssignmentModel
            {
                DeveloperId = bd.DeveloperId,
                FullName = bd.Developer.FullName,
                DeveloperCode = bd.Developer.DeveloperCode,
                IsAssigned = true
            })
            .ToListAsync();

        return Result<List<BatchAssignmentModel>>.Success(assigned, "Assigned developers loaded.");

        #endregion
    }

    private async Task<Result<List<BatchAssignmentModel>>> LoadAvailableDevelopersAsync(
        List<BatchAssignmentModel> assigned)
    {
        #region Load Available Developers

        var assignedIds = assigned.Select(a => a.DeveloperId).ToList();
        var globallyAssignedIds = await dbContext.BatchDevelopers
            .Select(bd => bd.DeveloperId)
            .Distinct()
            .ToListAsync();

        var available = await dbContext.Developers
            .Where(d => (d.IsActive ?? true) &&
                        !assignedIds.Contains(d.Id) &&
                        !globallyAssignedIds.Contains(d.Id))
            .Select(d => new BatchAssignmentModel
            {
                DeveloperId = d.Id,
                FullName = d.FullName,
                DeveloperCode = d.DeveloperCode,
                IsAssigned = false
            })
            .ToListAsync();

        return Result<List<BatchAssignmentModel>>.Success(available, "Available developers loaded.");

        #endregion
    }
}
