using DevTrack.Database.Entities;
using Microsoft.Extensions.Logging;

namespace DevTrack.Features.TrainingManagement.Batches;

public partial class BatchService : IBatchService
{
    private readonly DevTrackDbContext dbContext;

    private readonly ILogger<BatchService> logger;

    public BatchService(DevTrackDbContext db, ILogger<BatchService> logger)
    {
        #region Batch Service

        dbContext = db;
        this.logger = logger;

        #endregion
    }
}
