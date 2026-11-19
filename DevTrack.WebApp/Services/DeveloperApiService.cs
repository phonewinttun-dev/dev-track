using DevTrack.Features.TrainingManagement.Developers;
using DevTrack.Shared;

namespace DevTrack.WebApp.Services;

public class DeveloperApiService : IDeveloperService
{
    private readonly IDeveloperApiClient _api;

    public DeveloperApiService(IDeveloperApiClient api)
    {
        _api = api;
    }

    public async Task<PagedResult<DeveloperResponse>> GetDevelopersAsync(PaginationRequest request)
    {
        return await _api.GetDevelopersAsync(request);
    }
    public async Task<Result<DeveloperDetailResponse>> GetDeveloperByIdAsync(int id)
    {
        return await _api.GetDeveloperByIdAsync(id);
    }
    public async Task<Result<DeveloperResponse>> CreateDeveloperAsync(DeveloperRequest request)
    {
        return await _api.CreateDeveloperAsync(request);
    }
}
