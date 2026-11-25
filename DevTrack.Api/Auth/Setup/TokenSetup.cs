using DevTrack.Shared.Security;
using Microsoft.Extensions.Options;

namespace DevTrack.Api.Auth;

public partial class TokenSetup(
    IOptions<AccessTokenOptions> options,
    IHostEnvironment environment,
    ILogger<TokenSetup> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            #region Load the signing key before accepting requests

            var settings = options.Value;

            if (string.IsNullOrWhiteSpace(settings.SigningKey))
            {
                settings.SigningKey = await LoadSigningKeyAsync(settings.KeyFile, cancellationToken);
            }

            if (Convert.FromBase64String(settings.SigningKey).Length < SigningKeySize)
            {
                throw new InvalidOperationException("The JWT signing key must contain at least 32 random bytes encoded as Base64.");
            }

            logger.LogInformation("InitializeAsync => Token signing key is ready.");

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "InitializeAsync => Token setup failed.");
            throw;
        }
    }
}
