using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Shared;

namespace DevTrack.WebApp.Services;

public class BatchApiClient : ApiClientBase, IBatchApiClient
{
    public BatchApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public async Task<PagedResult<BatchResponse>> GetBatchesAsync(PaginationRequest request)
    {
        return await GetAsync<PagedResult<BatchResponse>>(WithPagination("/api/batches", request));
    }

    public async Task<Result<BatchResponse>> GetBatchByIdAsync(int id)
    {
        return await GetAsync<Result<BatchResponse>>($"/api/batches/{id}");
    }

    public async Task<Result<BatchResponse>> CreateBatchAsync(BatchRequest request)
    {
        return await PostAsync<BatchRequest, Result<BatchResponse>>("/api/batches", request);
    }

    public async Task<Result<List<BatchAssignmentModel>>> GetBatchDevelopersAsync(int id)
    {
        return await GetAsync<Result<List<BatchAssignmentModel>>>($"/api/batches/{id}/developers");
    }

    public async Task<Result> UpdateBatchAssignmentsAsync(int id, List<int> selectedDeveloperIds)
    {
        return await PostAsync<List<int>, Result>($"/api/batches/{id}/assignments", selectedDeveloperIds);
    }
}
