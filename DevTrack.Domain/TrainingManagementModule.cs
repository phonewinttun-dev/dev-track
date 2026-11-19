using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Features.TrainingManagement.Dashboard;
using DevTrack.Features.TrainingManagement.Developers;
using DevTrack.Features.TrainingManagement.Training;
using Microsoft.Extensions.DependencyInjection;

namespace DevTrack.Domain;

public static class TrainingManagementModule
{
    public static IServiceCollection AddTrainingManagementModule(this IServiceCollection services)
    {
        services.AddScoped<IBatchService, BatchService>();
        services.AddScoped<IDeveloperService, DeveloperService>();
        services.AddScoped<ITrainingService, TrainingService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
