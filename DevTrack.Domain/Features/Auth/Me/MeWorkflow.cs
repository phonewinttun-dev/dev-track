using DevTrack.Shared;
using Microsoft.Extensions.Logging;

namespace DevTrack.Features.Access.Auth;

public partial class UserAccessService
{
    public async Task<Result<MeResponse>> GetMeAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            #region Load the current active user's profile and grants

            var user = await FindMeAsync(userId, cancellationToken);

            if (user is null)
            {
                return Result<MeResponse>.Failure("The current user is no longer active.");
            }

            logger.LogInformation("GetMeAsync => Current user details loaded.");
            return Result<MeResponse>.Success(user);

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetMeAsync => Current user lookup failed.");
            throw;
        }
    }
}
