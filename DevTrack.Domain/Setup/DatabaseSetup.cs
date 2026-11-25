using DevTrack.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevTrack.Domain;

public partial class DatabaseSetup(DevTrackDbContext db, ILogger<DatabaseSetup> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            #region Create the database and add missing startup data

            await db.Database.EnsureCreatedAsync(cancellationToken);
            await using var transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(cancellationToken)
                : null;

            await AddPermissionsAsync(cancellationToken);
            await AddDefaultUsersAsync(cancellationToken);
            await DbInitializer.InitializeAsync(db, cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            logger.LogInformation("InitializeAsync => Database setup completed.");

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "InitializeAsync => Database setup failed.");
            throw;
        }
    }
}
