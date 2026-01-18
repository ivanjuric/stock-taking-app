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
public sealed class HomeController(AppDbContext context, IStockTakingService stockTakingService) : Controller
{
    public async Task<IActionResult> Index() =>
        User.IsInRole("Admin") 
            ? await AdminDashboard() 
            : await WorkerDashboard();

    private async Task<IActionResult> AdminDashboard()
    {
        ViewData["Title"] = "Dashboard";

        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var model = new AdminDashboardViewModel
        {
            TotalProducts = await context.Products.CountAsync(),
            TotalLocations = await context.Locations.CountAsync(),
            PendingStockTakings = await context.StockTakings
                .CountAsync(st => st.Status == StockTakingStatus.Requested),
            InProgressStockTakings = await context.StockTakings
                .CountAsync(st => st.Status == StockTakingStatus.InProgress),
            CompletedThisWeek = await context.StockTakings
                .CountAsync(st => st.Status == StockTakingStatus.Completed && st.CompletedAt >= weekAgo),
            TotalDiscrepancies = await context.StockTakingItems
                .CountAsync(i => i.StockTaking.Status == StockTakingStatus.Completed 
                    && i.CountedQuantity.HasValue 
                    && i.CountedQuantity != i.ExpectedQuantity),
            RecentStockTakings = await stockTakingService.GetRecentStockTakingsAsync(5),
            DiscrepancyAlerts = await stockTakingService.GetDiscrepancyAlertsAsync(5)
        };

        return View("AdminDashboard", model);
    }

    private async Task<IActionResult> WorkerDashboard()
    {
        ViewData["Title"] = "My Tasks";

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        var myTasks = await stockTakingService.GetWorkerStockTakingsAsync(userId);

        var model = new WorkerDashboardViewModel
        {
            AssignedTasks = myTasks.Count(t => t.Status == StockTakingStatus.Requested),
            InProgressTasks = myTasks.Count(t => t.Status == StockTakingStatus.InProgress),
            CompletedThisWeek = await context.StockTakings
                .CountAsync(st => st.Assignments.Any(a => a.UserId == userId)
                    && st.Status == StockTakingStatus.Completed
                    && st.CompletedAt >= weekAgo),
            MyTasks = myTasks
        };

        return View("WorkerDashboard", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
}
