using DevTrack.Database.Entities;
using Microsoft.Extensions.Logging;

namespace DevTrack.Features.TrainingManagement.Developers;

public partial class DeveloperService : IDeveloperService
{
    private readonly DevTrackDbContext dbContext;

    private readonly ILogger<DeveloperService> logger;

    public DeveloperService(DevTrackDbContext db, ILogger<DeveloperService> logger)
    {
        #region Developer Service

        dbContext = db;
        this.logger = logger;

        #endregion
    }
}
