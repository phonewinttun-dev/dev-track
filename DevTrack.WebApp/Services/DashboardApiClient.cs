using DevTrack.Features.TrainingManagement.Dashboard;
using DevTrack.Shared;

namespace DevTrack.WebApp.Services;

public class DashboardApiClient : ApiClientBase, IDashboardApiClient
{
    public DashboardApiClient(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    public async Task<Result<DashboardResponse>> GetDashboardDataAsync()
    {
        return await GetAsync<Result<DashboardResponse>>("/api/dashboard");
    }

    public async Task<Result<List<BatchSummaryModel>>> GetDashboardReportAsync()
    {
        return await GetAsync<Result<List<BatchSummaryModel>>>("/api/dashboard/report");
    }
}
