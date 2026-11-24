using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.TrainingManagement.Developers;

public partial class DeveloperService
{
    private async Task<Result<(List<DeveloperResponse> Developers, int TotalCount)>> LoadDeveloperPageAsync(
        PaginationRequest request)
    {
        #region Load Developer Page

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

        return Result<(List<DeveloperResponse> Developers, int TotalCount)>.Success(
            (developers, totalCount),
            "Developer page loaded.");

        #endregion
    }
}
