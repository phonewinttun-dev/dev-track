using DevTrack.Database.Entities;

namespace DevTrack.Domain.Tests;

public class DbInitializerTests
{
    [Fact]
    public async Task InitializeAsync_SeedsRelatedRecordsOnlyOnce()
    {
        using var context = TestDbContextFactory.Create();

        await DbInitializer.InitializeAsync(context);
        await DbInitializer.InitializeAsync(context);

        Assert.Equal(8, context.Developers.Count());
        Assert.Equal(2, context.Batches.Count());
        Assert.Equal(8, context.BatchDevelopers.Count());
        Assert.Equal(11, context.TrainingCalendars.Count());
        Assert.Equal(12, context.AttendanceRecords.Count());
        Assert.Equal(2, context.LeaveRecords.Count());
        Assert.Equal(3, context.AssignmentSubmissions.Count());
        var developerIds = context.Developers.Select(developer => developer.Id).ToHashSet();
        var batchIds = context.Batches.Select(batch => batch.Id).ToHashSet();
        Assert.All(context.BatchDevelopers, assignment =>
        {
            Assert.Contains(assignment.DeveloperId, developerIds);
            Assert.Contains(assignment.BatchId, batchIds);
        });
    }

    [Fact]
    public async Task InitializeAsync_PreservesExistingData()
    {
        using var context = TestDbContextFactory.Create();
        context.Developers.Add(new Developer { FullName = "Existing developer" });
        await context.SaveChangesAsync();

        await DbInitializer.InitializeAsync(context);

        Assert.Equal("Existing developer", Assert.Single(context.Developers).FullName);
        Assert.Empty(context.Batches);
        Assert.Empty(context.AttendanceRecords);
    }
}
