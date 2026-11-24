using DevTrack.WebApp.Services;
using DevTrack.WebApp.Auth;
using DevTrack.Shared.Security;
using DevTrack.Features.TrainingManagement.Training;
using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.WebApp.Controllers;

public class AttendanceController : Controller
{
    private readonly ITrainingService _trainingService;
    private readonly IBatchService _batchService;

    public AttendanceController(ITrainingService trainingService, IBatchService batchService)
    {
        #region Attendance Controller

        _trainingService = trainingService;
        _batchService = batchService;

        #endregion
    }

    [Permission(Permissions.TrainingRead)]
    public async Task<IActionResult> Index(int? batchId, int pageAt = 1)
    {
        #region Index

        var batches = await _batchService.GetBatchesAsync(new PaginationRequest { PageSize = 100 });


        if (batches.IsFailure)
        {
            var failureView = View("WorkflowFailure", batches.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        ViewBag.Batches = batches.Data;

        if (batchId.HasValue)
        {
            var summaryResult = await _trainingService.GetAttendanceSummaryAsync(batchId.Value, new PaginationRequest { PageNumber = pageAt });


            if (summaryResult.IsFailure)
            {
                var failureView = View("WorkflowFailure", summaryResult.Message);
                failureView.StatusCode = 400;

                return failureView;
            }

            ViewBag.SelectedBatchId = batchId.Value;
            return View(summaryResult);
        }

        return View(null);

        #endregion
    }

    [Permission(Permissions.TrainingRead)]
    public async Task<IActionResult> Schedule(int batchId, int pageAt = 1)
    {
        #region Schedule

        var scheduleResult = await _trainingService.GetScheduleAsync(batchId, new PaginationRequest { PageNumber = pageAt, PageSize = 10 });


        if (scheduleResult.IsFailure)
        {
            var failureView = View("WorkflowFailure", scheduleResult.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        var batch = await _batchService.GetBatchByIdAsync(batchId);

        if (batch.IsFailure)
        {
            var failureView = View("WorkflowFailure", batch.Message);
            failureView.StatusCode = 404;

            return failureView;
        }

        ViewBag.Batch = batch.Data;

        return View(scheduleResult);

        #endregion
    }

    [Permission(Permissions.TrainingWrite)]
    public async Task<IActionResult> Mark(int batchId, string? date)
    {
        #region Mark

        DateOnly targetDate;
        if (string.IsNullOrEmpty(date))
        {
            targetDate = DateOnly.FromDateTime(DateTime.Now);
        }
        else if (!DateOnly.TryParse(date, out targetDate))
        {
            var failureView = View("WorkflowFailure", "Choose a valid attendance date.");
            failureView.StatusCode = 400;

            return failureView;
        }

        var batchResult = await _batchService.GetBatchByIdAsync(batchId);


        if (batchResult.IsFailure)
        {
            var failureView = View("WorkflowFailure", batchResult.Message);
            failureView.StatusCode = 404;

            return failureView;
        }

        var classDays = await _trainingService.GetClassDaysAsync(batchId);

        if (classDays.IsFailure)
        {
            var failureView = View("WorkflowFailure", classDays.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        var markingData = await _trainingService.GetAttendanceForDateAsync(batchId, targetDate);


        if (markingData.IsFailure)
        {
            var failureView = View("WorkflowFailure", markingData.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        ViewBag.Batch = batchResult.Data;
        ViewBag.ClassDays = classDays.Data;
        ViewBag.TargetDate = targetDate;

        return View(markingData.Data);

        #endregion
    }

    [HttpPost]
    [Permission(Permissions.TrainingWrite)]
    public async Task<IActionResult> Mark(DevTrack.Features.TrainingManagement.Training.BulkAttendanceRequest request)
    {
        #region Mark

        var result = await _trainingService.MarkBulkAttendanceAsync(request);

        if (result.IsFailure)
        {
            ModelState.AddModelError("", result.Message);

            var batchResult = await _batchService.GetBatchByIdAsync(request.BatchId);

            if (batchResult.IsFailure)
            {
                var failureView = View("WorkflowFailure", batchResult.Message);
                failureView.StatusCode = 404;

                return failureView;
            }

            var classDays = await _trainingService.GetClassDaysAsync(request.BatchId);

            if (classDays.IsFailure)
            {
                var failureView = View("WorkflowFailure", classDays.Message);
                failureView.StatusCode = 400;

                return failureView;
            }

            var markingData = await _trainingService.GetAttendanceForDateAsync(request.BatchId, request.TrainingDate);

            if (markingData.IsFailure)
            {
                var failureView = View("WorkflowFailure", markingData.Message);
                failureView.StatusCode = 400;

                return failureView;
            }

            ViewBag.Batch = batchResult.Data;
            ViewBag.ClassDays = classDays.Data;
            ViewBag.TargetDate = request.TrainingDate;

            return View(markingData.Data);
        }

        TempData["WorkflowSuccess"] = $"{result.Message} Batch ID: {request.BatchId}. Date: {request.TrainingDate:yyyy-MM-dd}. Markings: {request.Markings.Count}.";

        return RedirectToAction(nameof(Index), new { batchId = request.BatchId });

        #endregion
    }

    [Permission(Permissions.TrainingRead)]
    public async Task<IActionResult> Download(int batchId)
    {
        #region Download

        var result = await _trainingService.GetFullAttendanceSummaryAsync(batchId);

        if (result.IsFailure)
        {
            var failureView = View("WorkflowFailure", result.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        var batch = await _batchService.GetBatchByIdAsync(batchId);

        if (batch.IsFailure)
        {
            var failureView = View("WorkflowFailure", batch.Message);
            failureView.StatusCode = 404;

            return failureView;
        }

        var fileResult = CreateReport(result.Data!);

        if (fileResult.IsFailure)
        {
            var failureView = View("WorkflowFailure", fileResult.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        var fileName = $"Attendance_Report_{batch.Data?.BatchName}_{DateTime.Now:yyyyMMdd}.csv";

        return File(fileResult.Data!, "text/csv", fileName);

        #endregion
    }

    private static Result<byte[]> CreateReport(List<AttendanceSummaryResponse> rows)
    {
        #region Create Report

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Developer Code,Full Name,Total Classes,Present,Absent,Leave,Attendance %,Status");

        foreach (var item in rows)
        {
            builder.Append(Csv.Escape(item.DeveloperCode)).Append(',');
            builder.Append(Csv.Escape(item.FullName)).Append(',');
            builder.Append(FormattableString.Invariant(
                $"{item.TotalClasses},{item.PresentDays},{item.AbsentDays},{item.LeaveDays},{item.AttendancePercent}%,"));
            builder.AppendLine(Csv.Escape(item.Status));
        }

        return Result<byte[]>.Success(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "Report generated.");

        #endregion
    }
}
