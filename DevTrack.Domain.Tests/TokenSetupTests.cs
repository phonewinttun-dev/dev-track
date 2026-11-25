using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DevTrack.Domain.Tests;

public class TokenSetupTests
{
    [Theory]
    [InlineData("AQ==", typeof(InvalidOperationException))]
    [InlineData("invalid-base64", typeof(FormatException))]
    public async Task Startup_RejectsInvalidConfiguredSigningKeys(string key, Type expectedException)
    {
        await using var factory = ApiAuthenticationTests.CreateFactory().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["AccessToken:SigningKey"] = key })));

        Assert.Throws(expectedException, () =>
        {
            using var client = factory.CreateClient();
        });
    }

    [Fact]
    public async Task Startup_RejectsCorruptLocalKeyWithoutReplacingIt()
    {
        var keyFile = Path.Combine(Path.GetTempPath(), $"devtrack-key-test-{Guid.NewGuid():N}.key");
        byte[] original = [1, 2, 3];
        await File.WriteAllBytesAsync(keyFile, original);

        try
        {
            await using var factory = ApiAuthenticationTests.CreateFactory().WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["AccessToken:SigningKey"] = "",
                        ["AccessToken:KeyFile"] = keyFile
                    })));

            Assert.Throws<InvalidOperationException>(() =>
            {
                using var client = factory.CreateClient();
            });
            Assert.Equal(original, await File.ReadAllBytesAsync(keyFile));
        }
        finally
        {
            File.Delete(keyFile);
        }
    }
}
