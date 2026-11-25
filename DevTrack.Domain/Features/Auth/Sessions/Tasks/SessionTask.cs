using System.Security.Cryptography;
using System.Text;
using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Features.Access.Auth;

public partial class SessionService
{
    private static string HashToken(string token)
    {
        #region Compute the one-way token digest

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        #endregion
    }

    private async Task StoreSessionAsync(string token, int userId, CancellationToken cancellationToken)
    {
        #region Persist only the token hash and session metadata

        db.AccessSessions.Add(new AccessSession
        {
            TokenHash = HashToken(token),
            UserId = userId,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(options.Value.ExpiresMinutes)
        });
        await db.SaveChangesAsync(cancellationToken);

        #endregion
    }

    private async Task<Result<int>> FindSessionUserIdAsync(string token, CancellationToken cancellationToken)
    {
        #region Resolve a valid session and current active user

        if (!IsValidToken(token))
        {
            return Result<int>.Failure("Invalid access token.");
        }

        var tokenHash = HashToken(token);
        var now = DateTime.UtcNow;
        var userId = await db.AccessSessions.AsNoTracking()
            .Where(session => session.TokenHash == tokenHash
                && !session.IsRevoked
                && session.ExpiresAtUtc > now
                && session.User.IsActive)
            .Select(session => (int?)session.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (userId is null)
        {
            return Result<int>.Failure("Invalid access session.");
        }

        return Result<int>.Success(userId.Value);

        #endregion
    }

    private async Task<Result> RevokeSessionAsync(string token, CancellationToken cancellationToken)
    {
        #region Revoke the matching session

        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure("An access token is required.");
        }

        var tokenHash = HashToken(token);
        var session = await db.AccessSessions.SingleOrDefaultAsync(
            session => session.TokenHash == tokenHash, cancellationToken);

        if (session is not null)
        {
            session.IsRevoked = true;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();

        #endregion
    }
}
