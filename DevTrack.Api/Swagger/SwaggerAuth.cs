using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DevTrack.Api.Swagger;

public class SwaggerAuth : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        #region Describe protected endpoints without locking anonymous login

        if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            }
        ];

        #endregion
    }
}
