using DevTrack.Database.Entities;
using Microsoft.Extensions.Logging;

namespace DevTrack.Features.TrainingManagement.Training;

public partial class TrainingService : ITrainingService
{
    private readonly DevTrackDbContext dbContext;

    private readonly ILogger<TrainingService> logger;

    public TrainingService(DevTrackDbContext db, ILogger<TrainingService> logger)
    {
        #region Training Service

        dbContext = db;
        this.logger = logger;

        #endregion
    }
}
