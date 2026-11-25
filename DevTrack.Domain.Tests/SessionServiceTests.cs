using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using DevTrack.Database.Entities;
using DevTrack.Features.Access.Auth;
using DevTrack.Shared.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevTrack.Domain.Tests;

public class SessionServiceTests
{
    [Fact]
    public async Task CreateAsync_StoresOnlyHashAndIssuesIndependentTokens()
    {
        await using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var first = await service.CreateAsync(1);
        var second = await service.CreateAsync(1);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Data, second.Data);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(first.Data);
        var claim = Assert.Single(jwt.Payload);
        Assert.Equal("sid", claim.Key);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", Assert.IsType<string>(claim.Value));
        Assert.Equal(32, Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes((string)claim.Value).Length);
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(first.Data!)));
        var session = await db.AccessSessions.SingleAsync(session => session.TokenHash == expectedHash);
        Assert.NotEqual(first.Data, session.TokenHash);
        Assert.InRange(session.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(24), DateTime.UtcNow.AddMinutes(26));
        Assert.False(session.IsRevoked);
    }

    [Fact]
    public async Task ValidateAsync_ResolvesInternalUserIdAndRejectsTampering()
    {
        await using var db = TestDbContextFactory.Create();
        var user = new AppUser { Username = "private-user", DisplayName = "Private Name", Role = new AppRole { RoleName = "Reader" } };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var token = (await service.CreateAsync(user.Id)).Data!;
        db.ChangeTracker.Clear();

        var result = await service.ValidateAsync(token);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Data);
        var changedToken = (token[0] == 'A' ? "B" : "A") + token[1..];
        Assert.True((await service.ValidateAsync(changedToken)).IsFailure);
        Assert.True((await service.ValidateAsync("invalid")).IsFailure);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("disabled")]
    [InlineData("deleted")]
    public async Task ValidateAsync_RejectsInvalidSessions(string condition)
    {
        await using var db = TestDbContextFactory.Create();
        var user = new AppUser { Username = "user", Role = new AppRole { RoleName = "Reader" } };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var token = (await service.CreateAsync(user.Id)).Data!;
        var session = await db.AccessSessions.SingleAsync();
        switch (condition)
        {
            case "expired":
                session.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
                break;
            case "revoked":
                await service.RevokeAsync(token);
                break;
            case "disabled":
                user.IsActive = false;
                break;
            case "deleted":
                db.Users.Remove(user);
                break;
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.True((await service.ValidateAsync(token)).IsFailure);
    }

    [Fact]
    public async Task RevokeAsync_LeavesOtherSessionsActive()
    {
        await using var db = TestDbContextFactory.Create();
        var user = new AppUser { Username = "user", Role = new AppRole { RoleName = "Reader" } };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var first = (await service.CreateAsync(user.Id)).Data!;
        var second = (await service.CreateAsync(user.Id)).Data!;

        await service.RevokeAsync(first);
        await service.RevokeAsync(first);

        Assert.True((await service.ValidateAsync(first)).IsFailure);
        Assert.True((await service.ValidateAsync(second)).IsSuccess);
    }

    [Fact]
    public async Task ValidateAsync_PropagatesStoreFailureAndCancellation()
    {
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var token = (await service.CreateAsync(1)).Data!;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ValidateAsync(token, cancellation.Token));
        await db.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.ValidateAsync(token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid")]
    public async Task ValidateAsync_RejectsMissingOrMalformedTokensWithoutStoreLookup(string? token)
    {
        var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        await db.DisposeAsync();

        Assert.True((await service.ValidateAsync(token!)).IsFailure);
    }

    [Fact]
    public async Task SessionWorkflows_DoNotLogCredentialsHashesOrProfileData()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(new AppUser
        {
            Username = "private-user",
            DisplayName = "Private Name",
            Role = new AppRole { RoleName = "Private Role" }
        });
        await db.SaveChangesAsync();
        var logger = new SessionLogger();
        var service = new SessionService(db,
            Options.Create(new AccessTokenOptions { ExpiresMinutes = 25, SigningKey = AuthTestSettings.SigningKey }), logger);
        var token = (await service.CreateAsync(1)).Data!;
        var hash = (await db.AccessSessions.SingleAsync()).TokenHash;

        await service.ValidateAsync(token);
        await service.RevokeAsync(token);
        await service.ValidateAsync(token);

        var logs = string.Join("\n", logger.Messages);
        Assert.DoesNotContain(token, logs);
        var sessionId = new JwtSecurityTokenHandler().ReadJwtToken(token).Payload["sid"].ToString();
        Assert.DoesNotContain(sessionId!, logs);
        Assert.DoesNotContain(hash, logs);
        Assert.DoesNotContain("private-user", logs);
        Assert.DoesNotContain("Private Name", logs);
        Assert.DoesNotContain("Private Role", logs);
        Assert.All(logger.Messages, message => Assert.Matches("^(CreateAsync|ValidateAsync|RevokeAsync) =>", message));
    }

    [Theory]
    [InlineData("unsigned")]
    [InlineData("wrong-key")]
    [InlineData("extra-claim")]
    [InlineData("invalid-sid")]
    [InlineData("wrong-claim")]
    public async Task ValidateAsync_RejectsInvalidJwtEvenWhenItsHashExists(string condition)
    {
        await using var db = TestDbContextFactory.Create();
        var user = new AppUser { Username = "user", Role = new AppRole { RoleName = "Reader" } };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var payload = new JwtPayload { ["sid"] = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32)) };
        var key = condition == "wrong-key"
            ? RandomNumberGenerator.GetBytes(32)
            : Convert.FromBase64String(AuthTestSettings.SigningKey);
        var credentials = condition == "unsigned"
            ? null
            : new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        if (condition == "extra-claim")
        {
            payload["role"] = "Admin";
        }
        else if (condition == "invalid-sid")
        {
            payload["sid"] = "too-short";
        }
        else if (condition == "wrong-claim")
        {
            payload.Clear();
            payload["sub"] = user.Id.ToString();
        }

        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(credentials), payload));
        db.AccessSessions.Add(new AccessSession
        {
            TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))),
            UserId = user.Id,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        });
        await db.SaveChangesAsync();

        Assert.True((await CreateService(db).ValidateAsync(token)).IsFailure);
    }

    private sealed class SessionLogger : ILogger<SessionService>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private static SessionService CreateService(DevTrackDbContext db)
    {
        return new SessionService(db, Options.Create(new AccessTokenOptions { ExpiresMinutes = 25, SigningKey = AuthTestSettings.SigningKey }),
            NullLogger<SessionService>.Instance);
    }
}
