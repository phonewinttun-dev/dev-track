using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Dashboard;

public partial class DashboardService
{
    public async Task<Result<List<BatchSummaryModel>>> GetDashboardReportAsync()
    {
        try
        {
            #region Load Dashboard Report

            var loadDashboardReportResult = await LoadDashboardReportAsync();

            if (loadDashboardReportResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDashboardReportAsync => LoadDashboardReport failed. Reason={Reason}, NextAction=return failure",
                    loadDashboardReportResult.Message);

                return Result<List<BatchSummaryModel>>.Failure(loadDashboardReportResult.Message);
            }

            var batches = loadDashboardReportResult.Data!;

            logger.LogInformation(
                "GetDashboardReportAsync => LoadDashboardReport succeeded. Reason={Reason}, BatchCount={BatchCount}, NextAction=return successful outcome",
                loadDashboardReportResult.Message,
                batches.Count);

            #endregion

            return Result<List<BatchSummaryModel>>.Success(batches);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetDashboardReportAsync => Operation failed, NextAction=return failure");

            return Result<List<BatchSummaryModel>>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
