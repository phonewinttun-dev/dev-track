using DevTrack.Database.Entities;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Developers;

public partial class DeveloperService
{
    private async Task<Result> SaveDeveloperAsync(Developer developer)
    {
        #region Save Developer

        dbContext.Developers.Add(developer);
        await dbContext.SaveChangesAsync();

        return Result.Success("Developer saved.");

        #endregion
    }
}
