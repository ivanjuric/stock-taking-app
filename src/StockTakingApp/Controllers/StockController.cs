using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models.ViewModels;
using StockTakingApp.Mapping;

namespace StockTakingApp.Controllers;

[Authorize(Roles = "Admin")]
public sealed class StockController(AppDbContext context) : Controller
{
    private const int DefaultPageSize = 20;

    public async Task<IActionResult> Index(
        int? locationId,
        string? search,
        string? sortBy,
        bool sortDesc = false,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        ViewData["Title"] = "Stock Levels";

        var locations = await context.Locations
            .OrderBy(l => l.Code)
            .Select(l => new LocationViewModel
            {
                Id = l.Id,
                Code = l.Code,
                Name = l.Name
            })
            .ToListAsync();

        var query = context.Stocks
            .Include(s => s.Product)
            .Include(s => s.Location)
            .AsQueryable();

        // Location filter
        if (locationId.HasValue)
        {
            query = query.Where(s => s.LocationId == locationId.Value);
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(s =>
                s.Product.Name.ToLower().Contains(searchLower) ||
                s.Product.Sku.ToLower().Contains(searchLower) ||
                s.Location.Name.ToLower().Contains(searchLower) ||
                s.Location.Code.ToLower().Contains(searchLower));
        }

        // Get total count before pagination
        var totalItems = await query.CountAsync();

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "sku" => sortDesc ? query.OrderByDescending(s => s.Product.Sku) : query.OrderBy(s => s.Product.Sku),
            "product" => sortDesc ? query.OrderByDescending(s => s.Product.Name) : query.OrderBy(s => s.Product.Name),
            "category" => sortDesc ? query.OrderByDescending(s => s.Product.Category) : query.OrderBy(s => s.Product.Category),
            "quantity" => sortDesc ? query.OrderByDescending(s => s.Quantity) : query.OrderBy(s => s.Quantity),
            "updated" => sortDesc ? query.OrderByDescending(s => s.UpdatedAt) : query.OrderBy(s => s.UpdatedAt),
            _ => sortDesc 
                ? query.OrderByDescending(s => s.Location.Code).ThenByDescending(s => s.Product.Name) 
                : query.OrderBy(s => s.Location.Code).ThenBy(s => s.Product.Name) // default: location
        };

        // Pagination
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);

        var stocks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.ToViewModel())
            .ToListAsync();

        var pagedResult = new PagedResult<StockViewModel>
        {
            Items = stocks,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Search = search,
            SortBy = sortBy,
            SortDesc = sortDesc
        };

        var extraParams = locationId.HasValue ? $"locationId={locationId}" : null;

        var model = new StockListViewModel
        {
            Stocks = pagedResult,
            LocationId = locationId,
            LocationName = locationId.HasValue
                ? locations.FirstOrDefault(l => l.Id == locationId)?.Name
                : null,
            Locations = locations,
            Pagination = PaginationViewModel.FromPagedResult(pagedResult, "/stock", extraParams)
        };

        // Return partial for HTMX requests
        if (Request.Headers.ContainsKey("HX-Request"))
            return PartialView("_StockList", model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int quantity)
    {
        var stock = await context.Stocks
            .Include(s => s.Product)
            .Include(s => s.Location)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (stock is null)
            return NotFound();

        stock.Quantity = quantity;
        stock.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_StockRow", stock.ToViewModel());
        }

        return RedirectToAction(nameof(Index));
    }
}
