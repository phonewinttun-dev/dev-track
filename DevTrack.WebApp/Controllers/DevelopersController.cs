using DevTrack.WebApp.Auth;
using DevTrack.Shared.Security;
using DevTrack.Domain.Features.Developers;
using DevTrack.Domain.Features.Developers.Models;
using DevTrack.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DevTrack.WebApp.Controllers;

public class DevelopersController : Controller
{
    private readonly IDeveloperService _developerService;

    public DevelopersController(IDeveloperService developerService)
    {
        _developerService = developerService;
    }

    [Permission(Permissions.DevelopersRead)]
    public async Task<IActionResult> Index(int pageAt = 1)
    {
        var result = await _developerService.GetDevelopersAsync(new PaginationRequest { PageNumber = pageAt });
        return View(result);
    }

    [Permission(Permissions.DevelopersRead)]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _developerService.GetDeveloperByIdAsync(id);
        if (result.IsFailure || result.Data == null)
        {
            return NotFound();
        }

        return View(result.Data);
    }

    [Permission(Permissions.DevelopersWrite)]
    public IActionResult Create()
    {
        return View(new DeveloperRequest());
    }

    [HttpPost]
    [Permission(Permissions.DevelopersWrite)]
    public async Task<IActionResult> Create(DeveloperRequest request)
    {
        var result = await _developerService.CreateDeveloperAsync(request);
        if (result.IsSuccess)
        {
            return RedirectToAction(nameof(Index));
        }
        ModelState.AddModelError("", result.Message);
        return View(request);
    }
}
