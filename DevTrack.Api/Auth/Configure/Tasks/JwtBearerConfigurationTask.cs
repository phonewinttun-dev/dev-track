using System.Text;
using DevTrack.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DevTrack.Api.Auth;

public partial class ConfigureJwtBearerOptions
{
    private static Result CheckScheme(string? name)
    {
        #region Match the existing bearer scheme

        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return Result.Failure("The options belong to a different authentication scheme.");
        }

        return Result.Success("Bearer scheme matched.");

        #endregion
    }

    private TokenValidationParameters BuildValidationParameters()
    {
        #region Build the existing validation policy

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        return parameters;

        #endregion
    }
}
