using DevTrack.Domain.Features.Training.Models;
using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace DevTrack.Domain.Features.Training;

public class TrainingService : ITrainingService
{
    private readonly DevTrackDbContext dbContext;

    private readonly ILogger<TrainingService> logger;

    public TrainingService(DevTrackDbContext db, ILogger<TrainingService> logger)
    {
        dbContext = db;
        this.logger = logger;
    }

    public async Task<Result<List<DateOnly>>> GetClassDaysAsync(int batchId)
    {
        try
        {
            var dates = await dbContext.TrainingCalendars
                .Where(c => c.BatchId == batchId && c.IsAttendanceRequired == true)
                .Select(c => c.TrainingDate)
                .OrderByDescending(d => d)
                .ToListAsync();
            return Result<List<DateOnly>>.Success(dates);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetClassDaysAsync => Operation failed.");
            return Result<List<DateOnly>>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<PagedResult<TrainingCalendarResponse>> GetScheduleAsync(int batchId, PaginationRequest request)
    {
        try
        {
            var query = dbContext.TrainingCalendars
                .AsNoTracking()
                .Where(c => c.BatchId == batchId);

            var totalCount = await query.CountAsync();

            var calendar = await query
                .OrderBy(c => c.TrainingDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var dates = calendar.Select(c => c.TrainingDate).ToList();

            var markedDates = await dbContext.AttendanceRecords
                .Where(a => a.BatchId == batchId && dates.Contains(a.TrainingDate))
                .Select(a => a.TrainingDate)
                .Distinct()
                .ToListAsync();

            var result = calendar.Select(c => new TrainingCalendarResponse
            {
                Id = c.Id,
                BatchId = c.BatchId,
                TrainingDate = c.TrainingDate,
                DayType = c.DayType,
                IsAttendanceRequired = c.IsAttendanceRequired ?? false,
                AssignmentTitle = c.AssignmentTitle,
                AssignmentDueDate = c.AssignmentDueDate,
                Remark = c.Remark,
                IsAttendanceMarked = markedDates.Contains(c.TrainingDate)
            }).ToList();

            var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
            return PagedResult<TrainingCalendarResponse>.Success(result, pagination);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetScheduleAsync => Operation failed.");
            return PagedResult<TrainingCalendarResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<Result<BulkAttendanceRequest>> GetAttendanceForDateAsync(int batchId, DateOnly date)
    {
        try
        {
            var calendar = await dbContext.TrainingCalendars
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.BatchId == batchId && c.TrainingDate == date);

            if (calendar == null)
            {
                return Result<BulkAttendanceRequest>.Failure("Date not found in calendar.");
            }

            var batchDevelopers = await dbContext.BatchDevelopers
                .AsNoTracking()
                .Where(bd => bd.BatchId == batchId)
                .Select(bd => bd.Developer)
                .ToListAsync();

            var existingAttendance = await dbContext.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.BatchId == batchId && a.TrainingDate == date)
                .ToDictionaryAsync(a => a.DeveloperId);

            var request = new BulkAttendanceRequest
            {
                BatchId = batchId,
                TrainingDate = date,
                Markings = batchDevelopers.Select(d => new AttendanceMarkingModel
                {
                    DeveloperId = d.Id,
                    FullName = d.FullName,
                    Status = existingAttendance.TryGetValue(d.Id, out var marking) ? marking.Status : "Present",
                    Remark = marking is not null ? marking.Remark : ""
                }).OrderBy(m => m.FullName).ToList()
            };

            return Result<BulkAttendanceRequest>.Success(request);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetAttendanceForDateAsync => Operation failed.");
            return Result<BulkAttendanceRequest>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<Result> MarkBulkAttendanceAsync(BulkAttendanceRequest request)
    {
        try
        {
            var calendar = await dbContext.TrainingCalendars
                .FirstOrDefaultAsync(c => c.BatchId == request.BatchId && c.TrainingDate == request.TrainingDate);

            if (calendar == null)
            {
                return Result.Failure("Date not found in calendar.");
            }

            if (!(calendar.IsAttendanceRequired ?? false))
            {
                return Result.Failure("Attendance can only be marked on class days.");
            }

            if (request.Markings is not { Count: > 0 } ||
                request.Markings.Any(item => item is null ||
                    item.Status is not ("Present" or "Absent" or "Leave" or "Late")))
            {
                return Result.Failure("Submit at least one marking and use a valid attendance status.");
            }

            var submittedIds = request.Markings.Select(item => item.DeveloperId).ToHashSet();
            var rosterIds = await dbContext.BatchDevelopers
                .Where(assignment => assignment.BatchId == request.BatchId)
                .Select(assignment => assignment.DeveloperId)
                .ToListAsync();
            if (submittedIds.Count != request.Markings.Count || !submittedIds.SetEquals(rosterIds))
            {
                return Result.Failure("Submit exactly one mark for each developer assigned to the batch.");
            }

            var existing = await dbContext.AttendanceRecords
                .Where(a => a.BatchId == request.BatchId && a.TrainingDate == request.TrainingDate)
                .ToDictionaryAsync(a => a.DeveloperId);

            dbContext.AttendanceRecords.RemoveRange(existing.Values.Where(record => !submittedIds.Contains(record.DeveloperId)));

            foreach (var item in request.Markings)
            {
                if (existing.TryGetValue(item.DeveloperId, out var record))
                {
                    record.Status = item.Status;
                    record.Remark = item.Remark;
                    continue;
                }

                dbContext.AttendanceRecords.Add(new AttendanceRecord
                {
                    BatchId = request.BatchId,
                    DeveloperId = item.DeveloperId,
                    TrainingDate = request.TrainingDate,
                    Status = item.Status,
                    Remark = item.Remark,
                    CreatedAt = DateTime.Now
                });
            }

            await dbContext.SaveChangesAsync();
            return Result.Success("Attendance marked successfully.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "MarkBulkAttendanceAsync => Operation failed.");
            return Result.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<PagedResult<AttendanceSummaryResponse>> GetAttendanceSummaryAsync(int batchId, PaginationRequest request)
    {
        try
        {
            var totalClasses = await dbContext.TrainingCalendars
                .CountAsync(c => c.BatchId == batchId && (c.IsAttendanceRequired ?? false));
            var minAttendancePercent = await GetMinAttendancePercentAsync(batchId);

            var query = dbContext.BatchDevelopers
                .Where(bd => bd.BatchId == batchId)
                .Select(bd => new { bd.Developer.Id, bd.Developer.FullName, bd.Developer.DeveloperCode });

            var totalCount = await query.CountAsync();

            var developers = await query
                .OrderBy(bd => bd.FullName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var developerIds = developers.Select(d => d.Id).ToList();

            var attendance = await dbContext.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.BatchId == batchId && developerIds.Contains(a.DeveloperId))
                .ToListAsync();

            var attendanceByDeveloper = attendance.ToLookup(record => record.DeveloperId);
            var result = developers.Select(developer => CreateAttendanceSummary(
                developer.Id,
                developer.FullName,
                developer.DeveloperCode,
                attendanceByDeveloper[developer.Id],
                totalClasses,
                minAttendancePercent)).ToList();

            var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
            return PagedResult<AttendanceSummaryResponse>.Success(result, pagination);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetAttendanceSummaryAsync => Operation failed.");
            return PagedResult<AttendanceSummaryResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<Result<List<AttendanceSummaryResponse>>> GetFullAttendanceSummaryAsync(int batchId)
    {
        try
        {
            var totalClasses = await dbContext.TrainingCalendars
                .CountAsync(c => c.BatchId == batchId && (c.IsAttendanceRequired ?? false));
            var minAttendancePercent = await GetMinAttendancePercentAsync(batchId);

            var developers = await dbContext.BatchDevelopers
                .Where(bd => bd.BatchId == batchId)
                .Select(bd => new { bd.Developer.Id, bd.Developer.FullName, bd.Developer.DeveloperCode })
                .OrderBy(bd => bd.FullName)
                .ToListAsync();

            var attendance = await dbContext.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.BatchId == batchId)
                .ToListAsync();

            var attendanceByDeveloper = attendance.ToLookup(record => record.DeveloperId);
            var result = developers.Select(developer => CreateAttendanceSummary(
                developer.Id,
                developer.FullName,
                developer.DeveloperCode,
                attendanceByDeveloper[developer.Id],
                totalClasses,
                minAttendancePercent)).ToList();

            return Result<List<AttendanceSummaryResponse>>.Success(result);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetFullAttendanceSummaryAsync => Operation failed.");
            return Result<List<AttendanceSummaryResponse>>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    private async Task<int?> GetMinAttendancePercentAsync(int batchId)
    {
        return await dbContext.Batches.Where(batch => batch.Id == batchId)
            .Select(batch => batch.MinAttendancePercent)
            .SingleOrDefaultAsync();
    }

    private static AttendanceSummaryResponse CreateAttendanceSummary(
        int developerId,
        string fullName,
        string? developerCode,
        IEnumerable<AttendanceRecord> attendance,
        int totalClasses,
        int? minAttendancePercent)
    {
        var metrics = AttendanceMetrics.Calculate(attendance, totalClasses, minAttendancePercent);
        return new AttendanceSummaryResponse
        {
            DeveloperId = developerId,
            FullName = fullName,
            DeveloperCode = developerCode,
            TotalClasses = totalClasses,
            PresentDays = metrics.PresentDays,
            AbsentDays = metrics.AbsentDays,
            LeaveDays = metrics.LeaveDays,
            AttendancePercent = metrics.AttendancePercent,
            Status = metrics.Status
        };
    }
}
