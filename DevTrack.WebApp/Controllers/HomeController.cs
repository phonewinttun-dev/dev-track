using DevTrack.WebApp.Services;
using DevTrack.Shared;
using DevTrack.WebApp.Auth;
using DevTrack.Shared.Security;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DevTrack.Features.TrainingManagement.Dashboard;
using DevTrack.WebApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace DevTrack.WebApp.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;

    public HomeController(IDashboardService dashboardService)
    {
        #region Home Controller

        _dashboardService = dashboardService;

        #endregion
    }

    [Permission(Permissions.DashboardRead)]
    public async Task<IActionResult> Index()
    {
        #region Index

        var result = await _dashboardService.GetDashboardDataAsync();


        if (result.IsFailure)
        {
            var failureView = View("WorkflowFailure", result.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        return View(result.Data);

        #endregion
    }

    [Permission(Permissions.DashboardRead)]
    public async Task<IActionResult> Download()
    {
        #region Download

        var result = await _dashboardService.GetDashboardReportAsync();


        if (result.IsFailure)
        {
            var failureView = View("WorkflowFailure", result.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        var fileResult = CreateReport(result.Data!);

        if (fileResult.IsFailure)
        {
            var failureView = View("WorkflowFailure", fileResult.Message);
            failureView.StatusCode = 400;

            return failureView;
        }

        var fileName = $"Dashboard_Report_{DateTime.Now:yyyyMMdd}.csv";

        return File(fileResult.Data!, "text/csv", fileName);

        #endregion
    }

    [Permission(Permissions.DashboardRead)]
    public IActionResult Privacy()
    {
        #region Privacy

        return View();

        #endregion
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        #region Error

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        #endregion
    }

    private static Result<byte[]> CreateReport(List<BatchSummaryModel> rows)
    {
        #region Create Report

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Batch Name,Mentor,Developers");

        foreach (var item in rows)
        {
            builder.AppendLine(FormattableString.Invariant(
                $"{Csv.Escape(item.BatchName)},{Csv.Escape(item.MentorName)},{item.DeveloperCount}"));
        }

        return Result<byte[]>.Success(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "Report generated.");

        #endregion
    }
}
