using DevTrack.Database.Entities;
using DevTrack.Shared;
using DevTrack.Shared.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevTrack.Features.Access.Auth;

public partial class SessionService(
    DevTrackDbContext db,
    IOptions<AccessTokenOptions> options,
    ILogger<SessionService> logger)
{
    public async Task<Result<string>> CreateAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            #region Generate and persist the session

            var token = GenerateToken();
            await StoreSessionAsync(token, userId, cancellationToken);

            logger.LogInformation("CreateAsync => Access session created.");
            return Result<string>.Success(token);

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "CreateAsync => Session creation failed.");
            throw;
        }
    }

    public async Task<Result<int>> ValidateAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            #region Resolve the active session and current identity

            var result = await FindSessionUserIdAsync(token, cancellationToken);

            logger.LogInformation("ValidateAsync => Access session checked. IsValid={IsValid}", result.IsSuccess);

            return result;

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "ValidateAsync => Session validation failed.");
            throw;
        }
    }

    public async Task<Result> RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            #region Revoke the session

            var result = await RevokeSessionAsync(token, cancellationToken);

            logger.LogInformation("RevokeAsync => Session revocation checked. IsSuccess={IsSuccess}", result.IsSuccess);

            return result;

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "RevokeAsync => Session revocation failed.");
            throw;
        }
    }
}
