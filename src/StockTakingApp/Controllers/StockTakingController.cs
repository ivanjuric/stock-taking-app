using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models.Enums;
using StockTakingApp.Models.ViewModels;
using StockTakingApp.Services;

namespace StockTakingApp.Controllers;

[Authorize]
public class StockTakingController : Controller
{
    private readonly AppDbContext _context;
    private readonly IStockTakingService _stockTakingService;

    public StockTakingController(AppDbContext context, IStockTakingService stockTakingService)
    {
        _context = context;
        _stockTakingService = stockTakingService;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Stock Taking";

        var stockTakings = await _stockTakingService.GetRecentStockTakingsAsync(50);
        return View(stockTakings);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "New Stock Taking";

        var locations = await _context.Locations
            .OrderBy(l => l.Code)
            .Select(l => new LocationViewModel
            {
                Id = l.Id,
                Code = l.Code,
                Name = l.Name
            })
            .ToListAsync();

        var workers = await _context.Users
            .Where(u => u.Role == UserRole.Worker)
            .OrderBy(u => u.FullName)
            .Select(u => new WorkerViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email
            })
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
        if (model.AssignedWorkerIds == null || !model.AssignedWorkerIds.Any())
        {
            ModelState.AddModelError("AssignedWorkerIds", "At least one worker must be assigned");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableLocations = await _context.Locations
                .OrderBy(l => l.Code)
                .Select(l => new LocationViewModel { Id = l.Id, Code = l.Code, Name = l.Name })
                .ToListAsync();

            model.AvailableWorkers = await _context.Users
                .Where(u => u.Role == UserRole.Worker)
                .OrderBy(u => u.FullName)
                .Select(u => new WorkerViewModel { Id = u.Id, FullName = u.FullName, Email = u.Email })
                .ToListAsync();

            return View(model);
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var stockTaking = await _stockTakingService.CreateStockTakingAsync(
            model.LocationId,
            userId,
            model.AssignedWorkerIds,
            model.Notes);

        return RedirectToAction(nameof(Details), new { id = stockTaking.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var stockTaking = await _stockTakingService.GetStockTakingAsync(id);
        if (stockTaking == null)
            return NotFound();

        ViewData["Title"] = $"Stock Taking - {stockTaking.Location.Name}";

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");
        var isAssigned = stockTaking.Assignments.Any(a => a.UserId == userId);

        var model = new StockTakingDetailsViewModel
        {
            Id = stockTaking.Id,
            LocationName = stockTaking.Location.Name,
            LocationCode = stockTaking.Location.Code,
            Status = stockTaking.Status,
            StatusDisplay = stockTaking.Status.ToString(),
            StatusClass = stockTaking.Status switch
            {
                StockTakingStatus.Requested => "status-requested",
                StockTakingStatus.InProgress => "status-in-progress",
                StockTakingStatus.Completed => "status-completed",
                _ => ""
            },
            RequestedByName = stockTaking.RequestedBy.FullName,
            AssignedWorkers = stockTaking.Assignments.Select(a => a.User.FullName).ToList(),
            CreatedAt = stockTaking.CreatedAt,
            StartedAt = stockTaking.StartedAt,
            CompletedAt = stockTaking.CompletedAt,
            Notes = stockTaking.Notes,
            Items = stockTaking.Items.OrderBy(i => i.Product.Name).Select(i => new StockTakingItemViewModel
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductSku = i.Product.Sku,
                ProductName = i.Product.Name,
                ProductCategory = i.Product.Category,
                ExpectedQuantity = i.ExpectedQuantity,
                CountedQuantity = i.CountedQuantity,
                CountedAt = i.CountedAt,
                CountedByName = i.CountedBy?.FullName,
                Notes = i.Notes,
                IsCounted = i.CountedQuantity.HasValue,
                Variance = i.Variance,
                VariancePercent = i.VariancePercentage
            }).ToList(),
            TotalItems = stockTaking.Items.Count,
            CountedItems = stockTaking.Items.Count(i => i.CountedQuantity.HasValue),
            DiscrepancyCount = stockTaking.Items.Count(i => i.CountedQuantity.HasValue && i.CountedQuantity != i.ExpectedQuantity),
            CanStart = isAssigned && stockTaking.Status == StockTakingStatus.Requested,
            CanPerform = isAssigned && (stockTaking.Status == StockTakingStatus.Requested || stockTaking.Status == StockTakingStatus.InProgress),
            CanReview = isAdmin && stockTaking.Status == StockTakingStatus.Completed,
            CanAcceptCounts = isAdmin && stockTaking.Status == StockTakingStatus.Completed
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Perform(int id)
    {
        var stockTaking = await _stockTakingService.GetStockTakingAsync(id);
        if (stockTaking == null)
            return NotFound();

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        // Check if user is assigned
        if (!stockTaking.Assignments.Any(a => a.UserId == userId))
            return Forbid();

        // Start if not already started
        if (stockTaking.Status == StockTakingStatus.Requested)
        {
            await _stockTakingService.StartStockTakingAsync(id, userId);
            stockTaking = await _stockTakingService.GetStockTakingAsync(id);
        }

        ViewData["Title"] = $"Count Stock - {stockTaking!.Location.Name}";

        var model = new StockTakingPerformViewModel
        {
            Id = stockTaking.Id,
            LocationName = stockTaking.Location.Name,
            LocationCode = stockTaking.Location.Code,
            Status = stockTaking.Status,
            Items = stockTaking.Items.OrderBy(i => i.Product.Category).ThenBy(i => i.Product.Name).Select(i => new StockTakingItemViewModel
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductSku = i.Product.Sku,
                ProductName = i.Product.Name,
                ProductCategory = i.Product.Category,
                ExpectedQuantity = i.ExpectedQuantity,
                CountedQuantity = i.CountedQuantity,
                CountedAt = i.CountedAt,
                CountedByName = i.CountedBy?.FullName,
                Notes = i.Notes,
                IsCounted = i.CountedQuantity.HasValue,
                Variance = i.Variance,
                VariancePercent = i.VariancePercentage
            }).ToList(),
            TotalItems = stockTaking.Items.Count,
            CountedItems = stockTaking.Items.Count(i => i.CountedQuantity.HasValue)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CountItem(CountItemViewModel model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var success = await _stockTakingService.UpdateItemCountAsync(
            model.ItemId,
            model.CountedQuantity,
            userId,
            model.Notes);

        if (!success)
            return BadRequest();

        // Get updated item for partial view
        var item = await _context.StockTakingItems
            .Include(i => i.Product)
            .Include(i => i.CountedBy)
            .Include(i => i.StockTaking)
            .FirstOrDefaultAsync(i => i.Id == model.ItemId);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            var vm = new StockTakingItemViewModel
            {
                Id = item!.Id,
                ProductId = item.ProductId,
                ProductSku = item.Product.Sku,
                ProductName = item.Product.Name,
                ProductCategory = item.Product.Category,
                ExpectedQuantity = item.ExpectedQuantity,
                CountedQuantity = item.CountedQuantity,
                CountedAt = item.CountedAt,
                CountedByName = item.CountedBy?.FullName,
                Notes = item.Notes,
                IsCounted = item.CountedQuantity.HasValue,
                Variance = item.Variance,
                VariancePercent = item.VariancePercentage
            };
            
            // Get progress data for OOB swap
            var stockTaking = await _stockTakingService.GetStockTakingAsync(item.StockTakingId);
            var totalItems = stockTaking!.Items.Count;
            var countedItems = stockTaking.Items.Count(i => i.CountedQuantity.HasValue);
            
            var progressVm = new StockTakingPerformViewModel
            {
                Id = stockTaking.Id,
                TotalItems = totalItems,
                CountedItems = countedItems
            };
            
            // Return multiple partials - item row + OOB progress
            Response.Headers.Append("HX-Trigger-After-Swap", "itemCounted");
            return PartialView("_CountItemRowWithProgress", (vm, progressVm));
        }

        return RedirectToAction(nameof(Perform), new { id = item!.StockTakingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Verify user is assigned
        if (!await _stockTakingService.IsUserAssignedAsync(id, userId))
            return Forbid();

        var result = await _stockTakingService.CompleteStockTakingAsync(id);
        if (result == null)
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
        var stockTaking = await _stockTakingService.GetStockTakingAsync(id);
        if (stockTaking == null)
            return NotFound();

        if (stockTaking.Status != StockTakingStatus.Completed)
            return RedirectToAction(nameof(Details), new { id });

        ViewData["Title"] = $"Review - {stockTaking.Location.Name}";

        var model = new StockTakingReviewViewModel
        {
            Id = stockTaking.Id,
            LocationName = stockTaking.Location.Name,
            LocationCode = stockTaking.Location.Code,
            CompletedAt = stockTaking.CompletedAt,
            AssignedWorkers = stockTaking.Assignments.Select(a => a.User.FullName).ToList(),
            Items = stockTaking.Items.OrderBy(i => i.Product.Name).Select(i => new StockTakingItemViewModel
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductSku = i.Product.Sku,
                ProductName = i.Product.Name,
                ProductCategory = i.Product.Category,
                ExpectedQuantity = i.ExpectedQuantity,
                CountedQuantity = i.CountedQuantity,
                CountedAt = i.CountedAt,
                CountedByName = i.CountedBy?.FullName,
                Notes = i.Notes,
                IsCounted = i.CountedQuantity.HasValue,
                Variance = i.Variance,
                VariancePercent = i.VariancePercentage
            }).ToList(),
            TotalItems = stockTaking.Items.Count,
            MatchedItems = stockTaking.Items.Count(i => i.CountedQuantity.HasValue && i.CountedQuantity == i.ExpectedQuantity),
            DiscrepancyCount = stockTaking.Items.Count(i => i.CountedQuantity.HasValue && i.CountedQuantity != i.ExpectedQuantity)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AcceptCounts(int id)
    {
        var success = await _stockTakingService.AcceptCountsAsync(id);
        if (!success)
            return BadRequest();

        TempData["Success"] = "Stock levels have been updated with the counted quantities.";
        return RedirectToAction(nameof(Review), new { id });
    }
}
