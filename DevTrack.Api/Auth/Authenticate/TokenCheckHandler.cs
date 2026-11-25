using System.Text.Encodings.Web;
using DevTrack.Features.Access.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DevTrack.Api.Auth;

public partial class TokenCheckHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    SessionService sessions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Bearer";

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        #region Challenge with the bearer scheme

        Response.Headers.WWWAuthenticate = SchemeName;
        await base.HandleChallengeAsync(properties);

        #endregion
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        #region Read the bearer credential

        var headerResult = ReadBearerHeader();

        if (headerResult.IsFailure)
        {
            return AuthenticateResult.Fail(headerResult.Message);
        }

        var header = headerResult.Data;

        if (header is null)
        {
            return AuthenticateResult.NoResult();
        }

        #endregion

        #region Validate the bearer session

        var sessionResult = await sessions.ValidateAsync(header.Parameter!, Context.RequestAborted);

        if (sessionResult.IsFailure)
        {
            return AuthenticateResult.Fail("Invalid access session.");
        }

        #endregion

        #region Build the server-side principal

        var principal = BuildPrincipal(sessionResult.Data);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));

        #endregion
    }
}
