using System.Security.Cryptography;
using DevTrack.Database.Entities;
using DevTrack.Shared;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace DevTrack.Features.Access.Auth;

public partial class UserAccessService(DevTrackDbContext db, ILogger<UserAccessService> logger)
{
    private readonly ILogger<UserAccessService> logger = logger;

    private const int SaltSize = 16;
    private const int PasswordIterations = 100000;
    private const int PasswordKeySize = 32;

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = DerivePasswordHash(password, salt);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public async Task<AppUser?> LoginAsync(string username, string password)
    {
        try
        {
            #region Find the active user

            var userResult = await FindActiveUserAsync(username);

            if (userResult.IsFailure)
            {
                logger.LogInformation("LoginAsync => Authentication stopped. Reason={Reason}", userResult.Message);

                return null;
            }

            var user = userResult.Data!;

            logger.LogInformation("LoginAsync => Active user found. UserId={UserId}", user.Id);

            #endregion

            #region Verify the password

            var passwordResult = CheckPassword(password, user.PasswordHash);

            if (passwordResult.IsFailure)
            {
                logger.LogInformation("LoginAsync => Authentication stopped. Reason={Reason}", passwordResult.Message);

                return null;
            }

            logger.LogInformation("LoginAsync => Authentication succeeded. UserId={UserId}", user.Id);

            return user;

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "LoginAsync => Operation failed.");
            throw;
        }
    }

    public async Task<string[]> GetPermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetUserPermissions(userId).ToArrayAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetPermissionsAsync => Operation failed.");
            throw;
        }
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        try
        {
            return await GetUserPermissions(userId).AnyAsync(name => name == permission);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "HasPermissionAsync => Operation failed.");
            throw;
        }
    }

    private async Task<Result<AppUser>> FindActiveUserAsync(string username)
    {
        var user = await db.Users
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Username == username && user.IsActive);

        if (user is null)
        {
            return Result<AppUser>.Failure("Invalid username or password.");
        }

        return Result<AppUser>.Success(user);
    }

    private static Result CheckPassword(string password, string storedHash)
    {
        if (!VerifyPassword(password, storedHash))
        {
            return Result.Failure("Invalid username or password.");
        }

        return Result.Success("Credentials verified.");
    }

    private IQueryable<string> GetUserPermissions(int userId)
    {
        return from user in db.Users
               from rolePermission in db.RolePermissions
               where user.Id == userId && user.IsActive && rolePermission.RoleId == user.RoleId
               select rolePermission.Permission.PermissionName;
    }

    private static byte[] DerivePasswordHash(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password, salt, PasswordIterations, HashAlgorithmName.SHA256, PasswordKeySize);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var expectedHash = Convert.FromBase64String(parts[1]);
            var salt = Convert.FromBase64String(parts[0]);
            var actualHash = DerivePasswordHash(password, salt);
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch (FormatException)
        {
            // Malformed stored hashes cannot authenticate a user.
            return false;
        }
    }
}
