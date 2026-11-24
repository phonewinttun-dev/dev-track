using DevTrack.WebApp.Auth;
using DevTrack.Shared.Security;
using DevTrack.Features.TrainingManagement.Batches;
using DevTrack.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.WebApp.Controllers;

public class BatchesController : Controller
{
    private readonly IBatchService _batchService;

    public BatchesController(IBatchService batchService)
    {
        #region Batches Controller

        _batchService = batchService;

        #endregion
    }

    [Permission(Permissions.BatchesRead)]
    public async Task<IActionResult> Index(int pageAt = 1)
    {
        #region Index

        var result = await _batchService.GetBatchesAsync(new PaginationRequest { PageNumber = pageAt });


        if (result.IsFailure)
        {
            var failureView = View("WorkflowFailure", result.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        return View(result);

        #endregion
    }

    [Permission(Permissions.BatchesWrite)]
    public IActionResult Create()
    {
        #region Create

        return View(new BatchRequest { StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(2)) });

        #endregion
    }

    [HttpPost]
    [Permission(Permissions.BatchesWrite)]
    public async Task<IActionResult> Create(BatchRequest request)
    {
        #region Create

        var result = await _batchService.CreateBatchAsync(request);

        if (result.IsFailure)
        {
            ModelState.AddModelError("", result.Message);

            return View(request);
        }

        TempData["WorkflowSuccess"] = $"{result.Message} Batch ID: {result.Data!.Id}.";

        return RedirectToAction(nameof(Index));

        #endregion
    }

    [Permission(Permissions.BatchesWrite)]
    public async Task<IActionResult> ManageDevelopers(int id)
    {
        #region Manage Developers

        var batchResult = await _batchService.GetBatchByIdAsync(id);

        if (batchResult.IsFailure)
        {
            var failureView = View("WorkflowFailure", batchResult.Message);
            failureView.StatusCode = 404;

            return failureView;
        }

        var developersResult = await _batchService.GetBatchDevelopersAsync(id);


        if (developersResult.IsFailure)
        {
            var failureView = View("WorkflowFailure", developersResult.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        ViewBag.Batch = batchResult.Data;

        return View(developersResult.Data);

        #endregion
    }

    [HttpPost]
    [Permission(Permissions.BatchesWrite)]
    public async Task<IActionResult> ManageDevelopers(int id, List<int> selectedDeveloperIds)
    {
        #region Manage Developers

        var result = await _batchService.UpdateBatchAssignmentsAsync(id, selectedDeveloperIds);

        if (result.IsFailure)
        {
            ModelState.AddModelError("", result.Message);

            var batchResult = await _batchService.GetBatchByIdAsync(id);

            if (batchResult.IsFailure)
            {
                var failureView = View("WorkflowFailure", batchResult.Message);
                failureView.StatusCode = 404;

                return failureView;
            }

            var developersResult = await _batchService.GetBatchDevelopersAsync(id);

            if (developersResult.IsFailure)
            {
                var failureView = View("WorkflowFailure", developersResult.Message);
                failureView.StatusCode = 400;

                return failureView;
            }

            ViewBag.Batch = batchResult.Data;

            return View(developersResult.Data);
        }

        TempData["WorkflowSuccess"] = $"{result.Message} Batch ID: {id}. Assigned developers: {selectedDeveloperIds.Count}.";

        return RedirectToAction(nameof(Index));

        #endregion
    }
}
