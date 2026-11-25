using DevTrack.Features.Access.Auth;
using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Features.TrainingManagement.Dashboard;
using DevTrack.Features.TrainingManagement.Developers;
using DevTrack.Features.TrainingManagement.Training;
using Microsoft.Extensions.DependencyInjection;

namespace DevTrack.Domain;

public static class FeatureManager
{
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        #region Access services

        services.AddScoped<DatabaseSetup>();
        services.AddScoped<UserAccessService>();
        services.AddScoped<SessionService>();

        #endregion

        #region Training management services

        services.AddScoped<IBatchService, BatchService>();
        services.AddScoped<IDeveloperService, DeveloperService>();
        services.AddScoped<ITrainingService, TrainingService>();
        services.AddScoped<IDashboardService, DashboardService>();

        #endregion

        return services;
    }
}
