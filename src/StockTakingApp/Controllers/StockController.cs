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
    public async Task<IActionResult> Index(int? locationId)
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

        if (locationId.HasValue)
        {
            query = query.Where(s => s.LocationId == locationId.Value);
        }

        var stocks = await query
            .OrderBy(s => s.Location.Code)
            .ThenBy(s => s.Product.Name)
            .Select(s => s.ToViewModel())
            .ToListAsync();

        var model = new StockListViewModel
        {
            Stocks = stocks,
            LocationId = locationId,
            LocationName = locationId.HasValue 
                ? locations.FirstOrDefault(l => l.Id == locationId)?.Name 
                : null,
            Locations = locations
        };

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
