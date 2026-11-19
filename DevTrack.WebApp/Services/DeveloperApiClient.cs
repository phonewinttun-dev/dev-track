using DevTrack.Features.TrainingManagement.Developers;
using DevTrack.Shared;

namespace DevTrack.WebApp.Services;

public class DeveloperApiClient : ApiClientBase, IDeveloperApiClient
{
    public DeveloperApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public async Task<PagedResult<DeveloperResponse>> GetDevelopersAsync(PaginationRequest request)
    {
        return await GetAsync<PagedResult<DeveloperResponse>>(WithPagination("/api/developers", request));
    }

    public async Task<Result<DeveloperDetailResponse>> GetDeveloperByIdAsync(int id)
    {
        return await GetAsync<Result<DeveloperDetailResponse>>($"/api/developers/{id}");
    }

    public async Task<Result<DeveloperResponse>> CreateDeveloperAsync(DeveloperRequest request)
    {
        return await PostAsync<DeveloperRequest, Result<DeveloperResponse>>("/api/developers", request);
    }
}
