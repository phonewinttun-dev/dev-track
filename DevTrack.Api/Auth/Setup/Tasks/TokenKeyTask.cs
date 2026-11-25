using System.Security.Cryptography;

namespace DevTrack.Api.Auth;

public partial class TokenSetup
{
    private const int SigningKeySize = 32;

    private async Task<string> LoadSigningKeyAsync(string keyFile, CancellationToken cancellationToken)
    {
        #region Keep the same random key across restarts

        var path = Path.IsPathRooted(keyFile) ? keyFile : Path.Combine(environment.ContentRootPath, keyFile);
        await using var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite,
            FileShare.None, bufferSize: 4096, FileOptions.Asynchronous);
        byte[] key;

        if (stream.Length == 0)
        {
            key = RandomNumberGenerator.GetBytes(SigningKeySize);
            await stream.WriteAsync(key, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        else
        {
            if (stream.Length != SigningKeySize)
            {
                throw new InvalidOperationException("The local JWT signing key must contain exactly 32 bytes.");
            }

            key = new byte[SigningKeySize];
            await stream.ReadExactlyAsync(key, cancellationToken);
        }

        return Convert.ToBase64String(key);

        #endregion
    }
}
