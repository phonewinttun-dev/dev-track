using DevTrack.Database.Entities;
using DevTrack.Shared;
using DevTrack.Features.TrainingManagement.Training;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace DevTrack.Features.TrainingManagement.Developers;

public class DeveloperService : IDeveloperService
{
    private readonly DevTrackDbContext dbContext;

    private readonly ILogger<DeveloperService> logger;

    public DeveloperService(DevTrackDbContext db, ILogger<DeveloperService> logger)
    {
        dbContext = db;
        this.logger = logger;
    }

    public async Task<PagedResult<DeveloperResponse>> GetDevelopersAsync(PaginationRequest request)
    {
        try
        {
            var query = dbContext.Developers.AsNoTracking();

            var totalCount = await query.CountAsync();

            var developers = await query
                .OrderBy(d => d.FullName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(d => new DeveloperResponse
                {
                    Id = d.Id,
                    FullName = d.FullName,
                    DeveloperCode = d.DeveloperCode,
                    Email = d.Email,
                    IsActive = d.IsActive ?? true
                })
                .ToListAsync();

            var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
            return PagedResult<DeveloperResponse>.Success(developers, pagination);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetDevelopersAsync => Operation failed.");
            return PagedResult<DeveloperResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<Result<DeveloperDetailResponse>> GetDeveloperByIdAsync(int id)
    {
        try
        {
            var developer = await dbContext.Developers
                .AsNoTracking()
                .Where(d => d.Id == id)
                .Select(d => new DeveloperDetailResponse
                {
                    Id = d.Id,
                    FullName = d.FullName,
                    DeveloperCode = d.DeveloperCode,
                    Email = d.Email,
                    Phone = d.Phone,
                    IsActive = d.IsActive ?? true,
                    CreatedAt = d.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (developer == null)
            {
                return Result<DeveloperDetailResponse>.Failure("Developer not found.");
            }

            var currentBatch = await dbContext.BatchDevelopers
                .AsNoTracking()
                .Where(bd => bd.DeveloperId == id)
                .OrderByDescending(bd => bd.Batch.StartDate)
                .Select(bd => new DeveloperBatchSummary
                {
                    BatchId = bd.Batch.Id,
                    BatchName = bd.Batch.BatchName,
                    MentorName = bd.Batch.MentorName,
                    StartDate = bd.Batch.StartDate,
                    EndDate = bd.Batch.EndDate,
                    MinAttendancePercent = bd.Batch.MinAttendancePercent
                })
                .FirstOrDefaultAsync();

            developer.CurrentBatch = currentBatch;

            if (currentBatch == null)
            {
                return Result<DeveloperDetailResponse>.Success(developer);
            }

            var totalClasses = await dbContext.TrainingCalendars
                .AsNoTracking()
                .CountAsync(c => c.BatchId == currentBatch.BatchId && (c.IsAttendanceRequired ?? false));

            var attendance = await dbContext.AttendanceRecords
                .AsNoTracking()
                .Where(a => a.BatchId == currentBatch.BatchId && a.DeveloperId == id)
                .ToListAsync();

            var metrics = AttendanceMetrics.Calculate(attendance, totalClasses, currentBatch.MinAttendancePercent);

            developer.Attendance = new DeveloperAttendanceSummary
            {
                TotalClasses = totalClasses,
                PresentDays = metrics.PresentDays,
                AbsentDays = metrics.AbsentDays,
                LeaveDays = metrics.LeaveDays,
                LateDays = metrics.LateDays,
                AttendancePercent = metrics.AttendancePercent,
                Status = metrics.Status
            };

            developer.RecentAttendance = attendance
                .OrderByDescending(a => a.TrainingDate)
                .Take(10)
                .Select(a => new DeveloperAttendanceRecordResponse
                {
                    TrainingDate = a.TrainingDate,
                    Status = a.Status,
                    Remark = a.Remark
                })
                .ToList();

            return Result<DeveloperDetailResponse>.Success(developer);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetDeveloperByIdAsync => Operation failed.");
            return Result<DeveloperDetailResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }

    public async Task<Result<DeveloperResponse>> CreateDeveloperAsync(DeveloperRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return Result<DeveloperResponse>.Failure("Full name is required.");
            }

            var developer = new Developer
            {
                FullName = request.FullName,
                DeveloperCode = request.DeveloperCode,
                Email = request.Email,
                Phone = request.Phone,
                IsActive = request.IsActive,
                CreatedAt = DateTime.Now
            };

            dbContext.Developers.Add(developer);
            await dbContext.SaveChangesAsync();

            return Result<DeveloperResponse>.Success(new DeveloperResponse
            {
                Id = developer.Id,
                FullName = developer.FullName,
                DeveloperCode = developer.DeveloperCode,
                Email = developer.Email,
                IsActive = developer.IsActive ?? true
            }, "Developer created successfully.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "CreateDeveloperAsync => Operation failed.");
            return Result<DeveloperResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
