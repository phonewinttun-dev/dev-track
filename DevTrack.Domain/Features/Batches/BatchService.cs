using DevTrack.Domain.Features.Batches.Models;
using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace DevTrack.Domain.Features.Batches;

public class BatchService : IBatchService
{
    private readonly DevTrackDbContext dbContext;

    private readonly ILogger<BatchService> logger;

    public BatchService(DevTrackDbContext db, ILogger<BatchService> logger)
    {
        dbContext = db;
        this.logger = logger;
    }

    public async Task<PagedResult<BatchResponse>> GetBatchesAsync(PaginationRequest request)
    {
        try
        {
            var query = dbContext.Batches.AsNoTracking();

            var totalCount = await query.CountAsync();

            var batches = await query
                .OrderByDescending(b => b.StartDate)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(b => new BatchResponse
                {
                    Id = b.Id,
                    BatchName = b.BatchName,
                    MentorName = b.MentorName,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    TrainingMonths = b.TrainingMonths
                })
                .ToListAsync();

            var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
            return PagedResult<BatchResponse>.Success(batches, pagination);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetBatchesAsync => Operation failed.");
            return PagedResult<BatchResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<Result<BatchResponse>> GetBatchByIdAsync(int id)
    {
        try
        {
            var batch = await dbContext.Batches
                .Select(b => new BatchResponse
                {
                    Id = b.Id,
                    BatchName = b.BatchName,
                    MentorName = b.MentorName,
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    TrainingMonths = b.TrainingMonths
                })
                .FirstOrDefaultAsync(b => b.Id == id);

            if (batch == null)
            {
                return Result<BatchResponse>.Failure("Batch not found.");
            }

            return Result<BatchResponse>.Success(batch);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetBatchByIdAsync => Operation failed.");
            return Result<BatchResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }


    public async Task<Result<BatchResponse>> CreateBatchAsync(BatchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BatchName))
            {
                return Result<BatchResponse>.Failure("Batch name is required.");
            }

            var batch = new Batch
            {
                BatchName = request.BatchName,
                MentorName = request.MentorName,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                TrainingMonths = request.TrainingMonths,
                DaysPerWeek = request.DaysPerWeek,
                MinAttendancePercent = request.MinAttendancePercent,
                MonthlyLeaveAllowed = request.MonthlyLeaveAllowed,
                CreatedAt = DateTime.Now
            };

            GenerateCalendar(batch, request.SelectedDays);
            dbContext.Batches.Add(batch);
            await dbContext.SaveChangesAsync();

            return Result<BatchResponse>.Success(new BatchResponse
            {
                Id = batch.Id,
                BatchName = batch.BatchName,
                MentorName = batch.MentorName,
                StartDate = batch.StartDate,
                EndDate = batch.EndDate,
                TrainingMonths = batch.TrainingMonths
            }, "Batch created successfully.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "CreateBatchAsync => Operation failed.");
            return Result<BatchResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<Result<List<BatchAssignmentModel>>> GetBatchDevelopersAsync(int batchId)
    {
        try
        {
            var assigned = await dbContext.BatchDevelopers
                .Where(bd => bd.BatchId == batchId)
                .Select(bd => new BatchAssignmentModel
                {
                    DeveloperId = bd.DeveloperId,
                    FullName = bd.Developer.FullName,
                    DeveloperCode = bd.Developer.DeveloperCode,
                    IsAssigned = true
                })
                .ToListAsync();

            var assignedIds = assigned.Select(a => a.DeveloperId).ToList();
            var globallyAssignedIds = await dbContext.BatchDevelopers
                .Select(bd => bd.DeveloperId)
                .Distinct()
                .ToListAsync();

            var available = await dbContext.Developers
                .Where(d => (d.IsActive ?? true) &&
                            !assignedIds.Contains(d.Id) &&
                            !globallyAssignedIds.Contains(d.Id))
                .Select(d => new BatchAssignmentModel
                {
                    DeveloperId = d.Id,
                    FullName = d.FullName,
                    DeveloperCode = d.DeveloperCode,
                    IsAssigned = false
                })
                .ToListAsync();

            var result = assigned.Concat(available).OrderBy(x => x.FullName).ToList();
            return Result<List<BatchAssignmentModel>>.Success(result);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetBatchDevelopersAsync => Operation failed.");
            return Result<List<BatchAssignmentModel>>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<Result> UpdateBatchAssignmentsAsync(int batchId, List<int> selectedDeveloperIds)
    {
        try
        {
            var existing = await dbContext.BatchDevelopers.Where(bd => bd.BatchId == batchId).ToListAsync();
            dbContext.BatchDevelopers.RemoveRange(existing);

            foreach (var developerId in selectedDeveloperIds)
            {
                dbContext.BatchDevelopers.Add(new BatchDeveloper
                {
                    BatchId = batchId,
                    DeveloperId = developerId
                });
            }

            await dbContext.SaveChangesAsync();
            return Result.Success("Assignments updated successfully.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "UpdateBatchAssignmentsAsync => Operation failed.");
            return Result.Failure("Unable to complete the operation. Please try again.");
        }
    }

    private static void GenerateCalendar(Batch batch, List<DayOfWeek> trainingDays)
    {
        for (var day = batch.StartDate.DayNumber; day <= batch.EndDate.DayNumber; day++)
        {
            var currentDate = DateOnly.FromDayNumber(day);
            var isClassDay = trainingDays.Contains(currentDate.DayOfWeek);
            var calendarEntry = new TrainingCalendar
            {
                TrainingDate = currentDate,
                DayType = isClassDay ? "Class Day" : "Holiday",
                Remark = isClassDay ? "Regular Training Session" : "Weekend/Holiday",
                IsAttendanceRequired = isClassDay
            };

            batch.TrainingCalendars.Add(calendarEntry);
        }
    }
}
