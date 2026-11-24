using DevTrack.Database.Entities;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Developers;

public partial class DeveloperService
{
    private Result<Developer> PrepareDeveloper(DeveloperRequest request)
    {
        #region Prepare Developer

        var developer = new Developer
        {
            FullName = request.FullName,
            DeveloperCode = request.DeveloperCode,
            Email = request.Email,
            Phone = request.Phone,
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now
        };

        return Result<Developer>.Success(developer, "Developer prepared.");

        #endregion
    }
}
