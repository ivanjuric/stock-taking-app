using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.ViewModels;
using StockTakingApp.Mapping;

namespace StockTakingApp.Controllers;

[Authorize(Roles = "Admin")]
public sealed class LocationsController(AppDbContext context) : Controller
{
    private const int DefaultPageSize = 20;

    public async Task<IActionResult> Index(
        string? search,
        string? sortBy,
        bool sortDesc = false,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        ViewData["Title"] = "Locations";

        var query = context.Locations.AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(l => 
                l.Name.ToLower().Contains(searchLower) || 
                l.Code.ToLower().Contains(searchLower) ||
                (l.Description != null && l.Description.ToLower().Contains(searchLower)));
        }

        // Get total count before pagination
        var totalItems = await query.CountAsync();

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "name" => sortDesc ? query.OrderByDescending(l => l.Name) : query.OrderBy(l => l.Name),
            "products" => sortDesc 
                ? query.OrderByDescending(l => l.Stocks.Select(s => s.ProductId).Distinct().Count()) 
                : query.OrderBy(l => l.Stocks.Select(s => s.ProductId).Distinct().Count()),
            "stock" => sortDesc 
                ? query.OrderByDescending(l => l.Stocks.Sum(s => s.Quantity)) 
                : query.OrderBy(l => l.Stocks.Sum(s => s.Quantity)),
            _ => sortDesc ? query.OrderByDescending(l => l.Code) : query.OrderBy(l => l.Code) // default: code
        };

        // Pagination
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);

        var locations = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LocationViewModel
            {
                Id = l.Id,
                Code = l.Code,
                Name = l.Name,
                Description = l.Description,
                CreatedAt = l.CreatedAt,
                ProductCount = l.Stocks.Select(s => s.ProductId).Distinct().Count(),
                TotalStock = l.Stocks.Sum(s => s.Quantity)
            })
            .ToListAsync();

        var pagedResult = new PagedResult<LocationViewModel>
        {
            Items = locations,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Search = search,
            SortBy = sortBy,
            SortDesc = sortDesc
        };

        var model = new LocationListViewModel
        {
            Locations = pagedResult,
            Pagination = PaginationViewModel.FromPagedResult(pagedResult, "/locations")
        };

        // Return partial for HTMX requests
        if (Request.Headers.ContainsKey("HX-Request"))
            return PartialView("_LocationList", model);

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Create Location";
        return View(new LocationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LocationViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await context.Locations.AnyAsync(l => l.Code == model.Code))
        {
            ModelState.AddModelError("Code", "A location with this code already exists");
            return View(model);
        }

        var location = new Location
        {
            Code = model.Code,
            Name = model.Name,
            Description = model.Description
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["Title"] = "Edit Location";

        var location = await context.Locations.FindAsync(id);
        if (location is null)
            return NotFound();

        return View(location.ToViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LocationViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var location = await context.Locations.FindAsync(id);
        if (location is null)
            return NotFound();

        if (await context.Locations.AnyAsync(l => l.Code == model.Code && l.Id != id))
        {
            ModelState.AddModelError("Code", "A location with this code already exists");
            return View(model);
        }

        location.UpdateFromViewModel(model);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var location = await context.Locations.FindAsync(id);
        if (location is null)
            return NotFound();

        context.Locations.Remove(location);
        await context.SaveChangesAsync();

        if (Request.Headers.ContainsKey("HX-Request"))
            return Ok();

        return RedirectToAction(nameof(Index));
    }
}
