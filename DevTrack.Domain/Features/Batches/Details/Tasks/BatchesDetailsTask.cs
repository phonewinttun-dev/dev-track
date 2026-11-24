using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    private async Task<Result<BatchResponse>> FindBatchAsync(int id)
    {
        #region Find Batch

        var batch = await dbContext.Batches
            .Select(b => new BatchResponse
            {
                Id = b.Id,
                BatchName = b.BatchName,
                MentorName = b.MentorName,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                TrainingMonths = b.TrainingMonths
            })
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch == null)
        {
            return Result<BatchResponse>.Failure("Batch not found.");
        }

        return Result<BatchResponse>.Success(batch, "Batch found.");

        #endregion
    }
}
