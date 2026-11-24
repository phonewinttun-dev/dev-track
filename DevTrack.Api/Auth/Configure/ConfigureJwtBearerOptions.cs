using DevTrack.Shared.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace DevTrack.Api.Auth;

public partial class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtOptions jwtOptions;
    private readonly ILogger<ConfigureJwtBearerOptions> logger;

    public ConfigureJwtBearerOptions(
        IOptions<JwtOptions> jwtOptions,
        ILogger<ConfigureJwtBearerOptions> logger)
    {
        #region Store configuration dependencies

        this.jwtOptions = jwtOptions.Value;
        this.logger = logger;

        #endregion
    }

    public void Configure(JwtBearerOptions options)
    {
        #region Forward the default options entry point

        Configure(Options.DefaultName, options);

        #endregion
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        try
        {
            #region Check the authentication scheme

            var schemeResult = CheckScheme(name);

            if (schemeResult.IsFailure)
            {
                logger.LogInformation(
                    "Configure => Configuration skipped. Reason={Reason}, NextAction=leave options unchanged",
                    schemeResult.Message);

                return;
            }

            logger.LogInformation(
                "Configure => Bearer scheme selected. Reason={Reason}, NextAction=build validation parameters",
                schemeResult.Message);

            #endregion

            #region Build and apply validation parameters

            var parameters = BuildValidationParameters();

            logger.LogInformation(
                "Configure => Parameters prepared. ClockSkew={ClockSkew}, NextAction=apply parameters",
                parameters.ClockSkew);

            options.TokenValidationParameters = parameters;

            #endregion
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Configure => JWT configuration failed.");
            throw;
        }
    }
}
