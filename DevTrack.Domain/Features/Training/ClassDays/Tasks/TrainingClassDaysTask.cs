using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService
{
    private async Task<Result<List<DateOnly>>> LoadClassDaysAsync(int batchId)
    {
        #region Load Class Days

        var dates = await dbContext.TrainingCalendars
            .Where(c => c.BatchId == batchId && c.IsAttendanceRequired == true)
            .Select(c => c.TrainingDate)
            .OrderByDescending(d => d)
            .ToListAsync();

        return Result<List<DateOnly>>.Success(dates, "Class dates loaded.");

        #endregion
    }
}
