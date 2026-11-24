using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Dashboard;

public partial class DashboardService
{
    private async Task<Result<List<BatchSummaryModel>>> LoadDashboardReportAsync()
    {
        #region Load Dashboard Report

        var batches = await dbContext.Batches
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BatchSummaryModel
            {
                Id = b.Id,
                BatchName = b.BatchName,
                MentorName = b.MentorName ?? "Unassigned",
                DeveloperCount = dbContext.BatchDevelopers.Count(bd => bd.BatchId == b.Id)
            })
            .ToListAsync();

        return Result<List<BatchSummaryModel>>.Success(batches, "Dashboard report loaded.");

        #endregion
    }
}
