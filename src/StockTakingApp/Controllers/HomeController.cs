using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models;
using StockTakingApp.Models.Enums;
using StockTakingApp.Models.ViewModels;
using StockTakingApp.Services;

namespace StockTakingApp.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly IStockTakingService _stockTakingService;

    public HomeController(AppDbContext context, IStockTakingService stockTakingService)
    {
        _context = context;
        _stockTakingService = stockTakingService;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin"))
        {
            return await AdminDashboard();
        }
        else
        {
            return await WorkerDashboard();
        }
    }

    private async Task<IActionResult> AdminDashboard()
    {
        ViewData["Title"] = "Dashboard";

        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var model = new AdminDashboardViewModel
        {
            TotalProducts = await _context.Products.CountAsync(),
            TotalLocations = await _context.Locations.CountAsync(),
            PendingStockTakings = await _context.StockTakings
                .CountAsync(st => st.Status == StockTakingStatus.Requested),
            InProgressStockTakings = await _context.StockTakings
                .CountAsync(st => st.Status == StockTakingStatus.InProgress),
            CompletedThisWeek = await _context.StockTakings
                .CountAsync(st => st.Status == StockTakingStatus.Completed && st.CompletedAt >= weekAgo),
            TotalDiscrepancies = await _context.StockTakingItems
                .CountAsync(i => i.StockTaking.Status == StockTakingStatus.Completed 
                    && i.CountedQuantity.HasValue 
                    && i.CountedQuantity != i.ExpectedQuantity),
            RecentStockTakings = await _stockTakingService.GetRecentStockTakingsAsync(5),
            DiscrepancyAlerts = await _stockTakingService.GetDiscrepancyAlertsAsync(5)
        };

        return View("AdminDashboard", model);
    }

    private async Task<IActionResult> WorkerDashboard()
    {
        ViewData["Title"] = "My Tasks";

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var myTasks = await _stockTakingService.GetWorkerStockTakingsAsync(userId);

        var model = new WorkerDashboardViewModel
        {
            AssignedTasks = myTasks.Count(t => t.Status == StockTakingStatus.Requested),
            InProgressTasks = myTasks.Count(t => t.Status == StockTakingStatus.InProgress),
            CompletedThisWeek = await _context.StockTakings
                .CountAsync(st => st.Assignments.Any(a => a.UserId == userId)
                    && st.Status == StockTakingStatus.Completed
                    && st.CompletedAt >= weekAgo),
            MyTasks = myTasks
        };

        return View("WorkerDashboard", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
