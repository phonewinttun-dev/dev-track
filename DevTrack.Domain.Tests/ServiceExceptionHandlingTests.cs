using Microsoft.Extensions.Logging.Abstractions;
using DevTrack.Database.Entities;
using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Features.TrainingManagement.Dashboard;
using DevTrack.Features.TrainingManagement.Developers;
using DevTrack.Features.TrainingManagement.Training;
using DevTrack.Features.Access.Auth;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevTrack.Domain.Tests;

public class ServiceExceptionHandlingTests
{
    [Theory]
    [InlineData("batches")]
    [InlineData("developers")]
    [InlineData("dashboard")]
    [InlineData("training")]
    public async Task ReadAsync_ReturnsSafeFailureWhenDatabaseIsUnavailable(string feature)
    {
        var context = TestDbContextFactory.Create();
        await context.DisposeAsync();

        Result result = feature switch
        {
            "batches" => await new BatchService(context, NullLogger<BatchService>.Instance).GetBatchesAsync(new PaginationRequest()),
            "developers" => await new DeveloperService(context, NullLogger<DeveloperService>.Instance).GetDeveloperByIdAsync(1),
            "dashboard" => await new DashboardService(context, NullLogger<DashboardService>.Instance).GetDashboardDataAsync(),
            _ => await new TrainingService(context, NullLogger<TrainingService>.Instance).GetClassDaysAsync(1)
        };

        Assert.True(result.IsFailure);
        Assert.Equal("Unable to complete the operation. Please try again.", result.Message);
    }

    [Fact]
    public async Task CreateDeveloperAsync_LogsDatabaseExceptionAndReturnsSafeFailure()
    {
        var failure = new DbUpdateException("Internal database details");
        using var context = new FailingSaveContext(failure);
        var logger = new RecordingLogger<DeveloperService>();
        var service = new DeveloperService(context, logger);

        var result = await service.CreateDeveloperAsync(new DeveloperRequest { FullName = "Developer" });

        Assert.True(result.IsFailure);
        Assert.DoesNotContain("Internal database details", result.Message);
        Assert.Same(failure, logger.Exception);
        Assert.Equal(LogLevel.Error, logger.Level);
        Assert.StartsWith("CreateDeveloperAsync =>", logger.Message);
    }

    [Fact]
    public async Task CreateBatchAsync_ReturnsSafeFailureOnSaveError()
    {
        using var context = new FailingSaveContext(new DbUpdateException("Save failed"));

        var date = new DateOnly(2026, 5, 1);
        var result = await new BatchService(context, NullLogger<BatchService>.Instance).CreateBatchAsync(new BatchRequest
        {
            BatchName = "Batch",
            StartDate = date,
            EndDate = date,
            SelectedDays = [date.DayOfWeek]
        });

        Assert.True(result.IsFailure);
        Assert.Empty(context.Batches);
        Assert.Empty(context.TrainingCalendars);
    }

    [Fact]
    public async Task CreateDeveloperAsync_PropagatesCancellationWithoutLoggingError()
    {
        var cancellation = new OperationCanceledException();
        using var context = new FailingSaveContext(cancellation);
        var logger = new RecordingLogger<DeveloperService>();
        var service = new DeveloperService(context, logger);

        var thrown = await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.CreateDeveloperAsync(new DeveloperRequest { FullName = "Developer" }));

        Assert.Same(cancellation, thrown);
        Assert.Null(logger.Exception);
    }

    [Fact]
    public async Task LoginAsync_LogsAndRethrowsDatabaseFailure()
    {
        var context = TestDbContextFactory.Create();
        await context.DisposeAsync();
        var logger = new RecordingLogger<UserAccessService>();

        var thrown = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => new UserAccessService(context, logger).LoginAsync("admin", "password"));

        Assert.Same(thrown, logger.Exception);
        Assert.StartsWith("LoginAsync =>", logger.Message);
        Assert.DoesNotContain("password", logger.Message);
    }

    private sealed class FailingSaveContext(Exception failure) : DevTrackDbContext(
        new DbContextOptionsBuilder<DevTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromException<int>(failure);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Keep failure-path tests independent of the scaffolded SQL Server configuration.
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public Exception? Exception { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public LogLevel Level { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Exception = exception;
            Level = logLevel;
            Message = formatter(state, exception);
        }
    }
}
