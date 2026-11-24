using System.Text.Json;
using DevTrack.Shared;
using DevTrack.Shared.Security;

namespace DevTrack.Domain.Tests;

public class ResultErrorCodeTests
{
    [Fact]
    public void Failure_WithoutErrorCodePreservesExistingJsonShape()
    {
        #region Serialize the existing failure contract

        var result = Result<string>.Failure("Failure reason.");
        var json = JsonSerializer.Serialize(result);

        #endregion

        #region Verify optional metadata does not change existing payloads

        Assert.DoesNotContain("ErrorCode", json);
        Assert.True(result.IsFailure);
        Assert.Null(result.Data);

        #endregion
    }

    [Fact]
    public void Failure_WithErrorCodePreservesClassificationAndHasNoSuccessValue()
    {
        #region Round-trip a classified failure

        var original = Result<string[]>.Failure("Authentication required.", AuthErrorCodes.Unauthorized);
        var json = JsonSerializer.Serialize(original);
        var result = JsonSerializer.Deserialize<Result<string[]>>(json);

        #endregion

        #region Verify error classification and failure invariants

        Assert.NotNull(result);
        Assert.True(result.IsFailure);
        Assert.Equal(original.Message, result.Message);
        Assert.Equal(AuthErrorCodes.Unauthorized, result.ErrorCode);
        Assert.Null(result.Data);

        #endregion
    }
}
