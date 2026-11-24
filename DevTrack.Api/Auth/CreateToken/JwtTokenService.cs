using System.Security.Claims;
using DevTrack.Shared.Security;
using DevTrack.Shared;
using Microsoft.Extensions.Options;

namespace DevTrack.Api.Auth;

public partial class JwtTokenService
{
    private readonly JwtOptions jwtOptions;
    private readonly ILogger<JwtTokenService> logger;

    public JwtTokenService(IOptions<JwtOptions> options, ILogger<JwtTokenService> logger)
    {
        #region Store token creation dependencies

        jwtOptions = options.Value;
        this.logger = logger;

        #endregion
    }

    public Result<string> CreateAdminToken(string username, string displayName)
    {
        #region Forward the existing admin identity to token creation

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, username),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Role, AuthConstants.AdminRole)
        ];

        return CreateToken(claims);

        #endregion
    }

    public Result<string> CreateToken(IEnumerable<Claim> claims)
    {
        try
        {
            #region Prepare signing credentials

            var issuedAt = DateTime.UtcNow;
            var credentialsResult = PrepareSigningCredentials();

            if (credentialsResult.IsFailure)
            {
                logger.LogError(
                    "CreateToken => Signing preparation failed. Reason={Reason}, NextAction=return failure",
                    credentialsResult.Message);

                return Result<string>.Failure(credentialsResult.Message, credentialsResult.ErrorCode);
            }

            var credentials = credentialsResult.Data!;

            logger.LogInformation(
                "CreateToken => Signing credentials prepared. Algorithm={Algorithm}, Reason={Reason}, NextAction=build token",
                credentials.Algorithm,
                credentialsResult.Message);

            #endregion

            #region Build the token

            var tokenResult = BuildToken(claims, credentials, issuedAt);

            if (tokenResult.IsFailure)
            {
                logger.LogError(
                    "CreateToken => Token preparation failed. Reason={Reason}, NextAction=return failure",
                    tokenResult.Message);

                return Result<string>.Failure(tokenResult.Message, tokenResult.ErrorCode);
            }

            var token = tokenResult.Data!;

            logger.LogInformation(
                "CreateToken => Token prepared. ExpiresAt={ExpiresAt}, Reason={Reason}, NextAction=serialize token",
                token.ValidTo,
                tokenResult.Message);

            #endregion

            #region Serialize and return the token

            var serializedResult = SerializeToken(token);

            if (serializedResult.IsFailure)
            {
                logger.LogError(
                    "CreateToken => Serialization failed. Reason={Reason}, NextAction=return failure",
                    serializedResult.Message);

                return Result<string>.Failure(serializedResult.Message, serializedResult.ErrorCode);
            }

            logger.LogInformation(
                "CreateToken => Token serialized. Reason={Reason}, NextAction=return token",
                serializedResult.Message);

            return serializedResult;

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "CreateToken => Token creation failed.");

            throw;
        }
    }
}
