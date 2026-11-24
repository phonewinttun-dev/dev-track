using DevTrack.Database.Entities;
using Microsoft.Extensions.Logging;

namespace DevTrack.Features.TrainingManagement.Dashboard;

public partial class DashboardService : IDashboardService
{
    private readonly DevTrackDbContext dbContext;

    private readonly ILogger<DashboardService> logger;

    public DashboardService(DevTrackDbContext db, ILogger<DashboardService> logger)
    {
        #region Dashboard Service

        dbContext = db;
        this.logger = logger;

        #endregion
    }
}
