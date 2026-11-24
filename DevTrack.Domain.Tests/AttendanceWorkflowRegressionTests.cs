using DevTrack.Database.Entities;
using DevTrack.Features.TrainingManagement.Training;
using Microsoft.Extensions.Logging;

namespace DevTrack.Domain.Tests;

public class AttendanceWorkflowRegressionTests
{
    [Theory]
    [InlineData(false, false, "Unknown", "Date not found in calendar.")]
    [InlineData(true, false, "Unknown", "Attendance can only be marked on class days.")]
    [InlineData(true, true, "Unknown", "Submit at least one marking and use a valid attendance status.")]
    [InlineData(true, true, "Present", "Submit exactly one mark for each developer assigned to the batch.")]
    public async Task MarkBulkAttendanceAsync_PreservesFailurePrecedenceAndSkipsWrites(
        bool hasCalendar,
        bool isAttendanceRequired,
        string status,
        string expectedReason)
    {
        #region Prepare independent failing prerequisites

        using var db = TestDbContextFactory.Create();
        var date = new DateOnly(2026, 5, 1);

        db.Batches.Add(new Batch
        {
            Id = 1,
            BatchName = "Batch",
            StartDate = date,
            EndDate = date
        });
        db.Developers.Add(new Developer
        {
            Id = 1,
            FullName = "Developer"
        });
        db.AttendanceRecords.Add(new AttendanceRecord
        {
            BatchId = 1,
            DeveloperId = 1,
            TrainingDate = date,
            Status = "Absent"
        });

        if (hasCalendar)
        {
            db.TrainingCalendars.Add(new TrainingCalendar
            {
                BatchId = 1,
                TrainingDate = date,
                DayType = isAttendanceRequired ? "Class Day" : "Holiday",
                IsAttendanceRequired = isAttendanceRequired
            });
        }

        await db.SaveChangesAsync();

        #endregion

        #region Run the main workflow

        var logger = new WorkflowLogger();
        var service = new TrainingService(db, logger);
        var result = await service.MarkBulkAttendanceAsync(new BulkAttendanceRequest
        {
            BatchId = 1,
            TrainingDate = date,
            Markings =
            [
                new AttendanceMarkingModel
                {
                    DeveloperId = 1,
                    Status = status
                }
            ]
        });

        #endregion

        #region Verify the original reason and immediate stop

        Assert.True(result.IsFailure);
        Assert.Equal(expectedReason, result.Message);
        Assert.Equal("Absent", Assert.Single(db.AttendanceRecords).Status);
        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Contains(expectedReason, logger.Messages[^1]);
        Assert.Contains("NextAction=return failure", logger.Messages[^1]);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("PrepareAttendance succeeded"));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("SaveAttendance succeeded"));

        #endregion
    }

    private sealed class WorkflowLogger : ILogger<TrainingService>
    {
        #region Capture stage outcomes

        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        #endregion
    }
}
