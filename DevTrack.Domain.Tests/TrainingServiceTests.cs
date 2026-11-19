using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;
using System.Text;
using DevTrack.Database.Entities;
using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Features.TrainingManagement.Training;
using DevTrack.Shared;
using DevTrack.WebApp.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Domain.Tests;

public class TrainingServiceTests
{
    [Fact]
    public async Task GetClassDays_ShouldReturnOnlyAttendanceRequiredDates()
    {
        using var db = TestDbContextFactory.Create();
        await SeedBatchAndDeveloper(db);
        db.TrainingCalendars.AddRange(
            new TrainingCalendar { BatchId = 1, TrainingDate = new DateOnly(2026, 5, 2), DayType = "Holiday", IsAttendanceRequired = false },
            new TrainingCalendar { BatchId = 1, TrainingDate = new DateOnly(2026, 5, 1), DayType = "Class Day", IsAttendanceRequired = true },
            new TrainingCalendar { BatchId = 1, TrainingDate = new DateOnly(2026, 5, 3), DayType = "Class Day", IsAttendanceRequired = true });
        await db.SaveChangesAsync();

        var service = new TrainingService(db, NullLogger<TrainingService>.Instance);
        var result = await service.GetClassDaysAsync(1);
        Assert.NotNull(result.Data);

        Assert.Equal(2, result.Data.Count);
        Assert.Equal(new DateOnly(2026, 5, 3), result.Data[0]);
        Assert.Equal(new DateOnly(2026, 5, 1), result.Data[1]);
    }

    [Fact]
    public async Task GetAttendanceForDate_ShouldPrefillExistingAndDefaults()
    {
        using var db = TestDbContextFactory.Create();
        await SeedBatchAndDeveloper(db);
        db.Developers.Add(new Developer { Id = 2, FullName = "Dev Two", IsActive = true });
        db.BatchDevelopers.Add(new BatchDeveloper { BatchId = 1, DeveloperId = 2 });
        db.TrainingCalendars.Add(new TrainingCalendar { BatchId = 1, TrainingDate = new DateOnly(2026, 5, 1), DayType = "Class Day", IsAttendanceRequired = true });
        db.AttendanceRecords.Add(new AttendanceRecord { BatchId = 1, DeveloperId = 1, TrainingDate = new DateOnly(2026, 5, 1), Status = "Late", Remark = "Traffic" });
        await db.SaveChangesAsync();

        var service = new TrainingService(db, NullLogger<TrainingService>.Instance);
        var result = await service.GetAttendanceForDateAsync(1, new DateOnly(2026, 5, 1));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Markings.Count);
        Assert.Contains(result.Data.Markings, x => x.DeveloperId == 1 && x.Status == "Late" && x.Remark == "Traffic");
        Assert.Contains(result.Data.Markings, x => x.DeveloperId == 2 && x.Status == "Present");
    }

    [Fact]
    public async Task MarkBulkAttendance_ShouldReplacePreviousRecords()
    {
        using var db = TestDbContextFactory.Create();
        await SeedBatchAndDeveloper(db);
        db.TrainingCalendars.Add(new TrainingCalendar { BatchId = 1, TrainingDate = new DateOnly(2026, 5, 1), DayType = "Class Day", IsAttendanceRequired = true });
        db.AttendanceRecords.Add(new AttendanceRecord { BatchId = 1, DeveloperId = 1, TrainingDate = new DateOnly(2026, 5, 1), Status = "Absent" });
        await db.SaveChangesAsync();

        var service = new TrainingService(db, NullLogger<TrainingService>.Instance);
        var result = await service.MarkBulkAttendanceAsync(new BulkAttendanceRequest
        {
            BatchId = 1,
            TrainingDate = new DateOnly(2026, 5, 1),
            Markings = [new AttendanceMarkingModel { DeveloperId = 1, FullName = "Dev One", Status = "Present" }]
        });

        Assert.True(result.IsSuccess);
        Assert.Single(db.AttendanceRecords);
        Assert.Equal("Present", db.AttendanceRecords.Single().Status);
    }

    [Fact]
    public async Task GetSchedule_ShouldMarkDatesWithRecordedAttendance()
    {
        using var db = TestDbContextFactory.Create();
        await SeedBatchAndDeveloper(db);
        db.TrainingCalendars.AddRange(
            new TrainingCalendar { BatchId = 1, TrainingDate = new DateOnly(2026, 5, 1), DayType = "Class Day", IsAttendanceRequired = true },
            new TrainingCalendar { BatchId = 1, TrainingDate = new DateOnly(2026, 5, 2), DayType = "Class Day", IsAttendanceRequired = true });
        db.AttendanceRecords.Add(new AttendanceRecord { BatchId = 1, DeveloperId = 1, TrainingDate = new DateOnly(2026, 5, 1), Status = "Present" });
        await db.SaveChangesAsync();

        var service = new TrainingService(db, NullLogger<TrainingService>.Instance);
        var result = await service.GetScheduleAsync(1, new PaginationRequest { PageNumber = 1, PageSize = 10 });

        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.Single(x => x.TrainingDate == new DateOnly(2026, 5, 1)).IsAttendanceMarked);
        Assert.False(result.Data.Single(x => x.TrainingDate == new DateOnly(2026, 5, 2)).IsAttendanceMarked);
    }

    [Theory]
    [InlineData(new int[] { }, "Present")]
    [InlineData(new[] { 1, 1 }, "Present")]
    [InlineData(new[] { 2 }, "Present")]
    [InlineData(new[] { 1 }, "Unknown")]
    public async Task MarkBulkAttendance_RejectsInvalidMarkingsAndPreservesExistingRecords(int[] developerIds, string status)
    {
        using var db = TestDbContextFactory.Create();
        await SeedBatchAndDeveloper(db);
        var date = new DateOnly(2026, 5, 1);
        db.TrainingCalendars.Add(new TrainingCalendar { BatchId = 1, TrainingDate = date, DayType = "Class Day", IsAttendanceRequired = true });
        db.AttendanceRecords.Add(new AttendanceRecord { BatchId = 1, DeveloperId = 1, TrainingDate = date, Status = "Absent" });
        await db.SaveChangesAsync();

        var service = new TrainingService(db, NullLogger<TrainingService>.Instance);
        var result = await service.MarkBulkAttendanceAsync(new BulkAttendanceRequest
        {
            BatchId = 1,
            TrainingDate = date,
            Markings = developerIds.Select(id => new AttendanceMarkingModel { DeveloperId = id, Status = status }).ToList()
        });

        Assert.True(result.IsFailure);
        Assert.Equal("Absent", Assert.Single(db.AttendanceRecords).Status);
    }

    [Fact]
    public async Task Download_QuotesNamesAndUsesInvariantPercentages()
    {
        using var db = TestDbContextFactory.Create();
        await SeedBatchAndDeveloper(db);
        var developer = await db.Developers.SingleAsync();
        developer.FullName = "Doe, \"Jane\"";
        var date = new DateOnly(2026, 5, 1);
        for (var offset = 0; offset < 3; offset++)
        {
            db.TrainingCalendars.Add(new TrainingCalendar
            {
                BatchId = 1, TrainingDate = date.AddDays(offset), DayType = "Class Day", IsAttendanceRequired = true
            });
        }

        db.AttendanceRecords.Add(new AttendanceRecord { BatchId = 1, DeveloperId = 1, TrainingDate = date, Status = "Present" });
        await db.SaveChangesAsync();
        var controller = new AttendanceController(
            new TrainingService(db, NullLogger<TrainingService>.Instance),
            new BatchService(db, NullLogger<BatchService>.Instance));

        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var file = Assert.IsType<FileContentResult>(await controller.Download(1));
            Assert.Contains("\"Doe, \"\"Jane\"\"\",3,1,0,0,33.33%,", Encoding.UTF8.GetString(file.FileContents));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private static async Task SeedBatchAndDeveloper(DevTrackDbContext db)
    {
        db.Batches.Add(new Batch { Id = 1, BatchName = "Batch", StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 6, 1) });
        db.Developers.Add(new Developer { Id = 1, FullName = "Dev One", IsActive = true });
        db.BatchDevelopers.Add(new BatchDeveloper { BatchId = 1, DeveloperId = 1 });
        await db.SaveChangesAsync();
    }
}
