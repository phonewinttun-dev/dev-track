using DevTrack.Domain.Features.Developers.Models;
using DevTrack.Domain.Features.Training.Models;
using Microsoft.Extensions.Logging.Abstractions;
using DevTrack.Database.Entities;
using DevTrack.Domain.Features.Developers;
using DevTrack.Domain.Features.Training;
using DevTrack.Shared;

namespace DevTrack.Domain.Tests;

public class AttendanceSummaryRegressionTests
{
    [Theory]
    [InlineData(10, 8, 80, "Eligible", null)]
    [InlineData(10, 8, 80, "Warning", 85)]
    [InlineData(10, 7, 70, "Warning", null)]
    [InlineData(10, 6, 60, "Not Eligible", null)]
    [InlineData(0, 0, 0, "Not Eligible", null)]
    [InlineData(3, 2, 66.67, "Not Eligible", null)]
    public async Task SummariesAsync_PreserveThresholdsAndCounts(
        int totalClasses, int presentDays, double expectedPercent, string expectedStatus, int? minAttendancePercent)
    {
        using var db = TestDbContextFactory.Create();
        var startDate = new DateOnly(2026, 5, 1);
        db.Batches.Add(new Batch { Id = 1, BatchName = "Batch", StartDate = startDate, MinAttendancePercent = minAttendancePercent });
        db.Developers.AddRange(
            new Developer { Id = 1, FullName = "Alpha" },
            new Developer { Id = 2, FullName = "Beta" });
        db.BatchDevelopers.AddRange(
            new BatchDeveloper { BatchId = 1, DeveloperId = 1 },
            new BatchDeveloper { BatchId = 1, DeveloperId = 2 });

        for (var day = 0; day < totalClasses; day++)
        {
            db.TrainingCalendars.Add(new TrainingCalendar
            {
                BatchId = 1, TrainingDate = startDate.AddDays(day),
                DayType = "Class Day", IsAttendanceRequired = true
            });
        }

        var statuses = Enumerable.Repeat("Present", presentDays)
            .Concat(["Absent", "Leave", "Late"]).ToArray();
        for (var day = 0; day < statuses.Length; day++)
        {
            db.AttendanceRecords.Add(new AttendanceRecord
            {
                BatchId = 1, DeveloperId = 1,
                TrainingDate = startDate.AddDays(day), Status = statuses[day]
            });
        }

        await db.SaveChangesAsync();
        var trainingService = new TrainingService(db, NullLogger<TrainingService>.Instance);
        var paged = await trainingService.GetAttendanceSummaryAsync(1, new PaginationRequest());
        var full = await trainingService.GetFullAttendanceSummaryAsync(1);
        var detail = await new DeveloperService(db, NullLogger<DeveloperService>.Instance).GetDeveloperByIdAsync(1);

        Assert.NotNull(full.Data);
        Assert.NotNull(detail.Data?.Attendance);
        var summary = Assert.Single(paged.Data, item => item.DeveloperId == 1);
        var fullSummary = Assert.Single(full.Data, item => item.DeveloperId == 1);
        Assert.Equal(presentDays, summary.PresentDays);
        Assert.Equal(1, summary.AbsentDays);
        Assert.Equal(1, summary.LeaveDays);
        Assert.Equal((decimal)expectedPercent, summary.AttendancePercent);
        Assert.Equal(expectedStatus, summary.Status);
        Assert.Equal(summary.AttendancePercent, fullSummary.AttendancePercent);
        Assert.Equal(summary.Status, fullSummary.Status);
        Assert.Equal(summary.AttendancePercent, detail.Data.Attendance.AttendancePercent);
        Assert.Equal(summary.Status, detail.Data.Attendance.Status);
        Assert.Equal(1, detail.Data.Attendance.LateDays);
        var otherDeveloper = Assert.Single(full.Data, item => item.DeveloperId == 2);
        Assert.Equal(0, otherDeveloper.PresentDays);
        Assert.Equal(0, otherDeveloper.AttendancePercent);
    }
}
