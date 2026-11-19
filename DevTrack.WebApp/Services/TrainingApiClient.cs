using DevTrack.Features.TrainingManagement.Training;
using DevTrack.Shared;

namespace DevTrack.WebApp.Services;

public class TrainingApiClient : ApiClientBase, ITrainingApiClient
{
    public TrainingApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public async Task<Result<List<DateOnly>>> GetClassDaysAsync(int batchId)
    {
        return await GetAsync<Result<List<DateOnly>>>($"/api/training/batch/{batchId}/class-days");
    }

    public async Task<PagedResult<TrainingCalendarResponse>> GetScheduleAsync(int batchId, PaginationRequest request)
    {
        return await GetAsync<PagedResult<TrainingCalendarResponse>>(WithPagination($"/api/training/batch/{batchId}/schedule", request));
    }

    public async Task<Result<BulkAttendanceRequest>> GetAttendanceForDateAsync(int batchId, string date)
    {
        return await GetAsync<Result<BulkAttendanceRequest>>($"/api/training/batch/{batchId}/attendance/{date}");
    }

    public async Task<Result> MarkBulkAttendanceAsync(BulkAttendanceRequest request)
    {
        return await PostAsync<BulkAttendanceRequest, Result>("/api/training/attendance/bulk", request);
    }

    public async Task<PagedResult<AttendanceSummaryResponse>> GetAttendanceSummaryAsync(int batchId, PaginationRequest request)
    {
        return await GetAsync<PagedResult<AttendanceSummaryResponse>>(WithPagination($"/api/training/batch/{batchId}/summary", request));
    }

    public async Task<Result<List<AttendanceSummaryResponse>>> GetFullAttendanceSummaryAsync(int batchId)
    {
        return await GetAsync<Result<List<AttendanceSummaryResponse>>>($"/api/training/batch/{batchId}/summary/full");
    }
}
