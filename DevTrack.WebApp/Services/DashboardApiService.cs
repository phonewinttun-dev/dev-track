using DevTrack.Features.TrainingManagement.Dashboard;
using DevTrack.Shared;

namespace DevTrack.WebApp.Services;

public class DashboardApiService : IDashboardService
{
    private readonly IDashboardApiClient _api;

    public DashboardApiService(IDashboardApiClient api)
    {
        _api = api;
    }

    public async Task<Result<DashboardResponse>> GetDashboardDataAsync()
    {
        return await _api.GetDashboardDataAsync();
    }

    public async Task<Result<List<BatchSummaryModel>>> GetDashboardReportAsync()
    {
        return await _api.GetDashboardReportAsync();
    }
}
