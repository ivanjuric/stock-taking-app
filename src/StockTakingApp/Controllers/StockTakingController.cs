using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models.Enums;
using StockTakingApp.Models.ViewModels;
using StockTakingApp.Services;
using StockTakingApp.Mapping;

namespace StockTakingApp.Controllers;

[Authorize]
public sealed class StockTakingController(AppDbContext context, IStockTakingService stockTakingService) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Stock Taking";

        var stockTakings = await stockTakingService.GetRecentStockTakingsAsync(50);
        return View(stockTakings);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "New Stock Taking";

        var locations = await context.Locations
            .OrderBy(l => l.Code)
            .Select(l => new LocationViewModel
            {
                Id = l.Id,
                Code = l.Code,
                Name = l.Name
            })
            .ToListAsync();

        var workers = await context.Users
            .Where(u => u.Role == UserRole.Worker)
            .OrderBy(u => u.FullName)
            .Select(u => u.ToWorkerViewModel())
            .ToListAsync();

        var model = new StockTakingCreateViewModel
        {
            AvailableLocations = locations,
            AvailableWorkers = workers
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(StockTakingCreateViewModel model)
    {
        if (model.AssignedWorkerIds is null || model.AssignedWorkerIds.Count == 0)
        {
            ModelState.AddModelError("AssignedWorkerIds", "At least one worker must be assigned");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableLocations = await context.Locations
                .OrderBy(l => l.Code)
                .Select(l => new LocationViewModel { Id = l.Id, Code = l.Code, Name = l.Name })
                .ToListAsync();

            model.AvailableWorkers = await context.Users
                .Where(u => u.Role == UserRole.Worker)
                .OrderBy(u => u.FullName)
                .Select(u => u.ToWorkerViewModel())
                .ToListAsync();

            return View(model);
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var stockTaking = await stockTakingService.CreateStockTakingAsync(
            model.LocationId,
            userId,
            model.AssignedWorkerIds ?? [],
            model.Notes);

        return RedirectToAction(nameof(Details), new { id = stockTaking.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var stockTaking = await stockTakingService.GetStockTakingAsync(id);
        if (stockTaking is null)
            return NotFound();

        ViewData["Title"] = $"Stock Taking - {stockTaking.Location.Name}";

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");
        var isAssigned = stockTaking.Assignments.Any(a => a.UserId == userId);

        return View(stockTaking.ToDetailsViewModel(isAdmin, isAssigned));
    }

    [HttpGet]
    public async Task<IActionResult> Perform(int id)
    {
        var stockTaking = await stockTakingService.GetStockTakingAsync(id);
        if (stockTaking is null)
            return NotFound();

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        // Check if user is assigned
        if (!stockTaking.Assignments.Any(a => a.UserId == userId))
            return Forbid();

        // Start if not already started
        if (stockTaking.Status == StockTakingStatus.Requested)
        {
            await stockTakingService.StartStockTakingAsync(id, userId);
            stockTaking = await stockTakingService.GetStockTakingAsync(id);
        }

        ViewData["Title"] = $"Count Stock - {stockTaking!.Location.Name}";

        return View(stockTaking.ToPerformViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CountItem(CountItemViewModel model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var success = await stockTakingService.UpdateItemCountAsync(
            model.ItemId,
            model.CountedQuantity,
            userId,
            model.Notes);

        if (!success)
            return BadRequest();

        // Get updated item for partial view
        var item = await context.StockTakingItems
            .Include(i => i.Product)
            .Include(i => i.CountedBy)
            .Include(i => i.StockTaking)
            .FirstOrDefaultAsync(i => i.Id == model.ItemId);

        if (item is null)
            return NotFound();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            var vm = item.ToViewModel();
            
            // Get progress data for OOB swap
            var stockTaking = await stockTakingService.GetStockTakingAsync(item.StockTakingId);
            if (stockTaking is null)
                return NotFound();
            
            var progressVm = new StockTakingPerformViewModel
            {
                Id = stockTaking.Id,
                TotalItems = stockTaking.Items.Count,
                CountedItems = stockTaking.Items.Count(i => i.CountedQuantity.HasValue)
            };
            
            // Return multiple partials - item row + OOB progress
            Response.Headers.Append("HX-Trigger-After-Swap", "itemCounted");
            return PartialView("_CountItemRowWithProgress", (vm, progressVm));
        }

        return RedirectToAction(nameof(Perform), new { id = item.StockTakingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Verify user is assigned
        if (!await stockTakingService.IsUserAssignedAsync(id, userId))
            return Forbid();

        var result = await stockTakingService.CompleteStockTakingAsync(id);
        if (result is null)
        {
            TempData["Error"] = "Cannot complete stock taking. All items must be counted.";
            return RedirectToAction(nameof(Perform), new { id });
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Review(int id)
    {
        var stockTaking = await stockTakingService.GetStockTakingAsync(id);
        if (stockTaking is null)
            return NotFound();

        if (stockTaking.Status != StockTakingStatus.Completed)
            return RedirectToAction(nameof(Details), new { id });

        ViewData["Title"] = $"Review - {stockTaking.Location.Name}";

        return View(stockTaking.ToReviewViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AcceptCounts(int id)
    {
        var success = await stockTakingService.AcceptCountsAsync(id);
        if (!success)
            return BadRequest();

        TempData["Success"] = "Stock levels have been updated with the counted quantities.";
        return RedirectToAction(nameof(Review), new { id });
    }
}
