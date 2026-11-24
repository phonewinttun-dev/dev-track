using Microsoft.Extensions.Logging;
using DevTrack.Shared;

namespace DevTrack.Features.TrainingManagement.Dashboard;

public partial class DashboardService
{
    public async Task<Result<DashboardResponse>> GetDashboardDataAsync()
    {
        try
        {
            #region Count Batches

            var countBatchesResult = await CountBatchesAsync();

            if (countBatchesResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDashboardDataAsync => CountBatches failed. Reason={Reason}, NextAction=return failure",
                    countBatchesResult.Message);

                return Result<DashboardResponse>.Failure(countBatchesResult.Message);
            }

            var totalBatches = countBatchesResult.Data;

            logger.LogInformation(
                "GetDashboardDataAsync => CountBatches succeeded. Reason={Reason}, TotalBatches={TotalBatches}, NextAction=count developers",
                countBatchesResult.Message,
                totalBatches);

            #endregion

            #region Count Developers

            var countDevelopersResult = await CountDevelopersAsync();

            if (countDevelopersResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDashboardDataAsync => CountDevelopers failed. Reason={Reason}, NextAction=return failure",
                    countDevelopersResult.Message);

                return Result<DashboardResponse>.Failure(countDevelopersResult.Message);
            }

            var totalDevelopers = countDevelopersResult.Data;

            logger.LogInformation(
                "GetDashboardDataAsync => CountDevelopers succeeded. Reason={Reason}, TotalDevelopers={TotalDevelopers}, NextAction=count active batches",
                countDevelopersResult.Message,
                totalDevelopers);

            #endregion

            #region Count Active Batches

            var countActiveBatchesResult = await CountActiveBatchesAsync();

            if (countActiveBatchesResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDashboardDataAsync => CountActiveBatches failed. Reason={Reason}, NextAction=return failure",
                    countActiveBatchesResult.Message);

                return Result<DashboardResponse>.Failure(countActiveBatchesResult.Message);
            }

            var activeBatches = countActiveBatchesResult.Data;

            logger.LogInformation(
                "GetDashboardDataAsync => CountActiveBatches succeeded. Reason={Reason}, ActiveBatches={ActiveBatches}, NextAction=calculate attendance rate",
                countActiveBatchesResult.Message,
                activeBatches);

            #endregion

            #region Calculate Attendance Rate

            var calculateAttendanceRateResult = await CalculateAttendanceRateAsync();

            if (calculateAttendanceRateResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDashboardDataAsync => CalculateAttendanceRate failed. Reason={Reason}, NextAction=return failure",
                    calculateAttendanceRateResult.Message);

                return Result<DashboardResponse>.Failure(calculateAttendanceRateResult.Message);
            }

            var overallRate = calculateAttendanceRateResult.Data;

            logger.LogInformation(
                "GetDashboardDataAsync => CalculateAttendanceRate succeeded. Reason={Reason}, AttendancePercent={AttendancePercent}, NextAction=load recent batches",
                calculateAttendanceRateResult.Message,
                overallRate);

            #endregion

            #region Load Recent Batches

            var loadRecentBatchesResult = await LoadRecentBatchesAsync();

            if (loadRecentBatchesResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDashboardDataAsync => LoadRecentBatches failed. Reason={Reason}, NextAction=return failure",
                    loadRecentBatchesResult.Message);

                return Result<DashboardResponse>.Failure(loadRecentBatchesResult.Message);
            }

            var recentBatches = loadRecentBatchesResult.Data!;

            logger.LogInformation(
                "GetDashboardDataAsync => LoadRecentBatches succeeded. Reason={Reason}, BatchCount={BatchCount}, NextAction=load attendance trends",
                loadRecentBatchesResult.Message,
                recentBatches.Count);

            #endregion

            #region Load Attendance Trends

            var loadAttendanceTrendsResult = await LoadAttendanceTrendsAsync();

            if (loadAttendanceTrendsResult.IsFailure)
            {
                logger.LogInformation(
                    "GetDashboardDataAsync => LoadAttendanceTrends failed. Reason={Reason}, NextAction=return failure",
                    loadAttendanceTrendsResult.Message);

                return Result<DashboardResponse>.Failure(loadAttendanceTrendsResult.Message);
            }

            var trends = loadAttendanceTrendsResult.Data!;

            logger.LogInformation(
                "GetDashboardDataAsync => LoadAttendanceTrends succeeded. Reason={Reason}, TrendCount={TrendCount}, NextAction=return successful outcome",
                loadAttendanceTrendsResult.Message,
                trends.Count);

            #endregion

            var response = new DashboardResponse
            {
                TotalBatches = totalBatches,
                TotalDevelopers = totalDevelopers,
                ActiveBatches = activeBatches,
                OverallAttendanceRate = Math.Round(overallRate, 1),
                RecentBatches = recentBatches,
                AttendanceTrends = trends
            };

            return Result<DashboardResponse>.Success(response);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "GetDashboardDataAsync => Operation failed, NextAction=return failure");

            return Result<DashboardResponse>.Failure("Unable to complete the operation. Please try again.");
        }
    }
}
