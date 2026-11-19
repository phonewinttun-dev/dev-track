using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Developers;

public interface IDeveloperService
{
    Task<PagedResult<DeveloperResponse>> GetDevelopersAsync(PaginationRequest request);
    Task<Result<DeveloperDetailResponse>> GetDeveloperByIdAsync(int id);
    Task<Result<DeveloperResponse>> CreateDeveloperAsync(DeveloperRequest request);
}
