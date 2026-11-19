using DevTrack.Features.TrainingManagement.Training;
using DevTrack.Shared;

namespace DevTrack.WebApp.Services;

public class TrainingApiService : ITrainingService
{
    private readonly ITrainingApiClient _api;

    public TrainingApiService(ITrainingApiClient api)
    {
        _api = api;
    }

    public async Task<Result<List<DateOnly>>> GetClassDaysAsync(int batchId)
    {
        return await _api.GetClassDaysAsync(batchId);
    }

    public async Task<PagedResult<TrainingCalendarResponse>> GetScheduleAsync(int batchId, PaginationRequest request)
    {
        return await _api.GetScheduleAsync(batchId, request);
    }
    
    public async Task<Result<BulkAttendanceRequest>> GetAttendanceForDateAsync(int batchId, DateOnly date)
    {
        return await _api.GetAttendanceForDateAsync(batchId, date.ToString("yyyy-MM-dd"));
    }

    public async Task<Result> MarkBulkAttendanceAsync(BulkAttendanceRequest request)
    {
        return await _api.MarkBulkAttendanceAsync(request);
    }
    
    public async Task<PagedResult<AttendanceSummaryResponse>> GetAttendanceSummaryAsync(int batchId, PaginationRequest request)
    {
        return await _api.GetAttendanceSummaryAsync(batchId, request);
    }

    public async Task<Result<List<AttendanceSummaryResponse>>> GetFullAttendanceSummaryAsync(int batchId)
    {
        return await _api.GetFullAttendanceSummaryAsync(batchId);
    }
}
