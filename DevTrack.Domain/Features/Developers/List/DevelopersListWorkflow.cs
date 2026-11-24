using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Developers;

public partial class DeveloperService
{
    public async Task<PagedResult<DeveloperResponse>> GetDevelopersAsync(PaginationRequest request)
    {
        try
        {
            #region Load Developer Page

            var loadDeveloperPageResult = await LoadDeveloperPageAsync(request);

            if (loadDeveloperPageResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDevelopersAsync => LoadDeveloperPage failed. Reason={Reason}, NextAction=return failure",
                    loadDeveloperPageResult.Message);

                return PagedResult<DeveloperResponse>.Failure(loadDeveloperPageResult.Message);
            }

            var (developers, totalCount) = loadDeveloperPageResult.Data;

            logger.LogInformation(
                "GetDevelopersAsync => LoadDeveloperPage succeeded. Reason={Reason}, DeveloperCount={DeveloperCount}, NextAction=return successful outcome",
                loadDeveloperPageResult.Message,
                developers.Count);

            #endregion

            var pagination = new Pagination(request.PageNumber, request.PageSize, totalCount);
            return PagedResult<DeveloperResponse>.Success(developers, pagination);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetDevelopersAsync => Operation failed, NextAction=return failure");

            return PagedResult<DeveloperResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
