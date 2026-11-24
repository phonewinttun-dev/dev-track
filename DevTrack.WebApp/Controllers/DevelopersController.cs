using DevTrack.WebApp.Auth;
using DevTrack.Shared.Security;
using DevTrack.Features.TrainingManagement.Developers;
using DevTrack.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.WebApp.Controllers;

public class DevelopersController : Controller
{
    private readonly IDeveloperService _developerService;

    public DevelopersController(IDeveloperService developerService)
    {
        #region Developers Controller

        _developerService = developerService;

        #endregion
    }

    [Permission(Permissions.DevelopersRead)]
    public async Task<IActionResult> Index(int pageAt = 1)
    {
        #region Index

        var result = await _developerService.GetDevelopersAsync(new PaginationRequest { PageNumber = pageAt });


        if (result.IsFailure)
        {
            var failureView = View("WorkflowFailure", result.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        return View(result);

        #endregion
    }

    [Permission(Permissions.DevelopersRead)]
    public async Task<IActionResult> Details(int id)
    {
        #region Details

        var result = await _developerService.GetDeveloperByIdAsync(id);


        if (result.IsFailure)
        {
            var failureView = View("WorkflowFailure", result.Message);
            failureView.StatusCode = 404;

            return failureView;
        }

        return View(result.Data);

        #endregion
    }

    [Permission(Permissions.DevelopersWrite)]
    public IActionResult Create()
    {
        #region Create

        return View(new DeveloperRequest());

        #endregion
    }

    [HttpPost]
    [Permission(Permissions.DevelopersWrite)]
    public async Task<IActionResult> Create(DeveloperRequest request)
    {
        #region Create

        var result = await _developerService.CreateDeveloperAsync(request);

        if (result.IsFailure)
        {
            ModelState.AddModelError("", result.Message);

            return View(request);
        }

        TempData["WorkflowSuccess"] = $"{result.Message} Developer ID: {result.Data!.Id}.";

        return RedirectToAction(nameof(Index));

        #endregion
    }
}
