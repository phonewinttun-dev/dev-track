using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService
{
    private async Task<Result<(List<BatchResponse> Batches, int TotalCount)>> LoadBatchPageAsync(
        PaginationRequest request)
    {
        #region Load Batch Page

        var query = dbContext.Batches.AsNoTracking();

        var totalCount = await query.CountAsync();

        var batches = await query
            .OrderByDescending(b => b.StartDate)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BatchResponse
            {
                Id = b.Id,
                BatchName = b.BatchName,
                MentorName = b.MentorName,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                TrainingMonths = b.TrainingMonths
            })
            .ToListAsync();

        return Result<(List<BatchResponse> Batches, int TotalCount)>.Success((batches, totalCount), "Batch page loaded.");

        #endregion
    }
}
