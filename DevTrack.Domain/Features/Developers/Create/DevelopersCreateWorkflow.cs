using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Developers;

public partial class DeveloperService
{
    public async Task<Result<DeveloperResponse>> CreateDeveloperAsync(DeveloperRequest request)
    {
        try
        {
            #region Validate Developer

            var validateDeveloperResult = ValidateDeveloper(request);

            if (validateDeveloperResult.IsFailure)
            {
                logger.LogInformation(
                    "CreateDeveloperAsync => ValidateDeveloper failed. Reason={Reason}, NextAction=return failure",
                    validateDeveloperResult.Message);

                return Result<DeveloperResponse>.Failure(validateDeveloperResult.Message);
            }

            logger.LogInformation(
                "CreateDeveloperAsync => ValidateDeveloper succeeded. Reason={Reason}, NextAction=prepare developer",
                validateDeveloperResult.Message);

            #endregion

            #region Prepare Developer

            var prepareDeveloperResult = PrepareDeveloper(request);

            if (prepareDeveloperResult.IsFailure)
            {
                logger.LogInformation(
                    "CreateDeveloperAsync => PrepareDeveloper failed. Reason={Reason}, NextAction=return failure",
                    prepareDeveloperResult.Message);

                return Result<DeveloperResponse>.Failure(prepareDeveloperResult.Message);
            }

            var developer = prepareDeveloperResult.Data!;

            logger.LogInformation(
                "CreateDeveloperAsync => PrepareDeveloper succeeded. Reason={Reason}, NextAction=save developer",
                prepareDeveloperResult.Message);

            #endregion

            #region Save Developer

            var saveDeveloperResult = await SaveDeveloperAsync(developer);

            if (saveDeveloperResult.IsFailure)
            {
                logger.LogInformation(
                    "CreateDeveloperAsync => SaveDeveloper failed. Reason={Reason}, NextAction=return failure",
                    saveDeveloperResult.Message);

                return Result<DeveloperResponse>.Failure(saveDeveloperResult.Message);
            }

            logger.LogInformation(
                "CreateDeveloperAsync => SaveDeveloper succeeded. Reason={Reason}, DeveloperId={DeveloperId}, NextAction=return successful outcome",
                saveDeveloperResult.Message,
                developer.Id);

            #endregion

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
            logger.LogError(exception, "CreateDeveloperAsync => Operation failed, NextAction=return failure");

            return Result<DeveloperResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
