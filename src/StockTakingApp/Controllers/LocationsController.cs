using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.ViewModels;

namespace StockTakingApp.Controllers;

[Authorize(Roles = "Admin")]
public class LocationsController : Controller
{
    private readonly AppDbContext _context;

    public LocationsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Title"] = "Locations";

        var query = _context.Locations.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l => l.Name.Contains(search) || l.Code.Contains(search));
        }

        var locations = await query
            .OrderBy(l => l.Code)
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

        var model = new LocationListViewModel
        {
            Locations = locations,
            SearchTerm = search
        };

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

        if (await _context.Locations.AnyAsync(l => l.Code == model.Code))
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

        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["Title"] = "Edit Location";

        var location = await _context.Locations.FindAsync(id);
        if (location == null)
            return NotFound();

        var model = new LocationViewModel
        {
            Id = location.Id,
            Code = location.Code,
            Name = location.Name,
            Description = location.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LocationViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var location = await _context.Locations.FindAsync(id);
        if (location == null)
            return NotFound();

        if (await _context.Locations.AnyAsync(l => l.Code == model.Code && l.Id != id))
        {
            ModelState.AddModelError("Code", "A location with this code already exists");
            return View(model);
        }

        location.Code = model.Code;
        location.Name = model.Name;
        location.Description = model.Description;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var location = await _context.Locations.FindAsync(id);
        if (location == null)
            return NotFound();

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync();

        if (Request.Headers.ContainsKey("HX-Request"))
            return Ok();

        return RedirectToAction(nameof(Index));
    }
}
