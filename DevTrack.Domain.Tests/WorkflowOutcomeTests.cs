using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Features.TrainingManagement.Training;
using DevTrack.Shared;
using DevTrack.WebApp.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DevTrack.Domain.Tests;

public class WorkflowOutcomeTests
{
    [Theory]
    [InlineData("batch", "batch")]
    [InlineData("class days", "batch,class days")]
    [InlineData("markings", "batch,class days,markings")]
    public async Task Mark_StopsAtFailedStepAndShowsItsReason(string failedStep, string expectedCalls)
    {
        #region Execute the workflow with a failing step

        var calls = new List<string>();
        var controller = new AttendanceController(
            new TrainingStub(calls, failedStep), new BatchStub(calls, failedStep));

        var result = await controller.Mark(7, "2026-05-01");

        #endregion

        #region Verify the failure is visible and later steps were skipped

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("WorkflowFailure", view.ViewName);
        Assert.Equal($"Unable to load {failedStep}.", view.Model);
        Assert.Equal(expectedCalls, string.Join(",", calls));

        #endregion
    }

    [Theory]
    [InlineData("schedule")]
    [InlineData("report")]
    [InlineData("batches")]
    public async Task Read_StopsBeforeDependentCallsWhenInitialStepFails(string failedStep)
    {
        #region Execute the failing read

        var calls = new List<string>();
        var controller = new AttendanceController(
            new TrainingStub(calls, failedStep), new BatchStub(calls, failedStep));

        var result = failedStep switch
        {
            "schedule" => await controller.Schedule(7),
            "report" => await controller.Download(7),
            _ => await controller.Index(7)
        };

        #endregion

        #region Verify no dependent read ran

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal($"Unable to load {failedStep}.", view.Model);
        Assert.Equal(new[] { failedStep }, calls);

        #endregion
    }

    [Fact]
    public async Task ManageDevelopers_ShowsRosterFailureInsteadOfAnEmptyList()
    {
        #region Execute the workflow

        var calls = new List<string>();
        var controller = new BatchesController(new BatchStub(calls, "roster"));

        var result = await controller.ManageDevelopers(7);

        #endregion

        #region Verify the reason reaches the UI

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("WorkflowFailure", view.ViewName);
        Assert.Equal("Unable to load roster.", view.Model);
        Assert.Equal(new[] { "batch", "roster" }, calls);

        #endregion
    }

    [Fact]
    public async Task CreateBatch_PreservesSuccessReasonAndIdAcrossRedirect()
    {
        #region Execute the successful creation

        var controller = new BatchesController(new BatchStub([], null))
        {
            TempData = CreateTempData()
        };

        var result = await controller.Create(new BatchRequest());

        #endregion

        #region Verify redirect feedback

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Batch created successfully. Batch ID: 7.", controller.TempData["WorkflowSuccess"]);

        #endregion
    }

    [Fact]
    public async Task Mark_PreservesSuccessReasonDateAndCountAcrossRedirect()
    {
        #region Execute the successful save

        var calls = new List<string>();
        var controller = new AttendanceController(new TrainingStub(calls, null), new BatchStub(calls, null))
        {
            TempData = CreateTempData()
        };

        var result = await controller.Mark(new BulkAttendanceRequest
        {
            BatchId = 7,
            TrainingDate = new DateOnly(2026, 5, 1),
            Markings = [new AttendanceMarkingModel { DeveloperId = 2 }]
        });

        #endregion

        #region Verify the save outcome is visible after redirect

        Assert.IsType<RedirectToActionResult>(result);
        var message = Assert.IsType<string>(controller.TempData["WorkflowSuccess"]);
        Assert.Contains("Attendance marked successfully.", message);
        Assert.Contains("Batch ID: 7", message);
        Assert.Contains("2026-05-01", message);
        Assert.Contains("Markings: 1", message);
        Assert.Equal(new[] { "save" }, calls);

        #endregion
    }

    private static TempDataDictionary CreateTempData()
    {
        #region Create redirect feedback storage

        return new TempDataDictionary(new DefaultHttpContext(), new MemoryTempDataProvider());

        #endregion
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        #region Store test feedback without external dependencies

        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }

        #endregion
    }

    private sealed class BatchStub(List<string> calls, string? failedStep) : IBatchService
    {
        #region Report controlled batch outcomes

        public async Task<PagedResult<BatchResponse>> GetBatchesAsync(PaginationRequest request)
        {
            calls.Add("batches");

            return await Task.FromResult(PagedResult<BatchResponse>.Failure("Unable to load batches."));
        }

        public async Task<Result<BatchResponse>> GetBatchByIdAsync(int id)
        {
            calls.Add("batch");

            return await Task.FromResult(failedStep == "batch"
                ? Result<BatchResponse>.Failure("Unable to load batch.")
                : Result<BatchResponse>.Success(new BatchResponse { Id = id }));
        }

        public async Task<Result<BatchResponse>> CreateBatchAsync(BatchRequest request)
        {
            return await Task.FromResult(Result<BatchResponse>.Success(
                new BatchResponse { Id = 7 }, "Batch created successfully."));
        }

        public async Task<Result<List<BatchAssignmentModel>>> GetBatchDevelopersAsync(int batchId)
        {
            calls.Add("roster");

            return await Task.FromResult(Result<List<BatchAssignmentModel>>.Failure("Unable to load roster."));
        }

        public Task<Result> UpdateBatchAssignmentsAsync(int batchId, List<int> selectedDeveloperIds)
        {
            throw new InvalidOperationException("Assignments must not be changed during a read.");
        }

        #endregion
    }

    private sealed class TrainingStub(List<string> calls, string? failedStep) : ITrainingService
    {
        #region Report controlled training outcomes

        public async Task<Result<List<DateOnly>>> GetClassDaysAsync(int batchId)
        {
            calls.Add("class days");

            return await Task.FromResult(failedStep == "class days"
                ? Result<List<DateOnly>>.Failure("Unable to load class days.")
                : Result<List<DateOnly>>.Success([]));
        }

        public async Task<PagedResult<TrainingCalendarResponse>> GetScheduleAsync(int batchId, PaginationRequest request)
        {
            calls.Add("schedule");

            return await Task.FromResult(PagedResult<TrainingCalendarResponse>.Failure("Unable to load schedule."));
        }

        public async Task<Result<BulkAttendanceRequest>> GetAttendanceForDateAsync(int batchId, DateOnly date)
        {
            calls.Add("markings");

            return await Task.FromResult(Result<BulkAttendanceRequest>.Failure("Unable to load markings."));
        }

        public async Task<Result> MarkBulkAttendanceAsync(BulkAttendanceRequest request)
        {
            calls.Add("save");

            return await Task.FromResult(Result.Success("Attendance marked successfully."));
        }

        public Task<PagedResult<AttendanceSummaryResponse>> GetAttendanceSummaryAsync(int batchId, PaginationRequest request)
        {
            throw new InvalidOperationException("Summary must not load after a failed batch lookup.");
        }

        public async Task<Result<List<AttendanceSummaryResponse>>> GetFullAttendanceSummaryAsync(int batchId)
        {
            calls.Add("report");

            return await Task.FromResult(Result<List<AttendanceSummaryResponse>>.Failure("Unable to load report."));
        }

        #endregion
    }
}
