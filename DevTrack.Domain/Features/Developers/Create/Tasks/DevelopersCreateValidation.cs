using Shared;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Developers;

public partial class DeveloperService
{
    private static Result ValidateDeveloper(DeveloperRequest request)
    {
        #region Validate Developer

        if (request.FullName.IsNullOrEmpty())
        {
            return Result.Failure("Full name is required.");
        }

        return Result.Success("Developer request is valid.");

        #endregion
    }
}
