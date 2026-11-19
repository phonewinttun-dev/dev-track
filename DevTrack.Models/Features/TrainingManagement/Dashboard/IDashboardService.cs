using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Dashboard;

public interface IDashboardService
{
    Task<Result<DashboardResponse>> GetDashboardDataAsync();
    Task<Result<List<BatchSummaryModel>>> GetDashboardReportAsync();
}
