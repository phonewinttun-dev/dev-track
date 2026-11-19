using DevTrack.Features.Access.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace DevTrack.Domain.Access;

public static class AccessModule
{
    public static IServiceCollection AddAccessModule(this IServiceCollection services)
    {
        return services.AddScoped<RbacService>();
    }
}
