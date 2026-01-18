using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models.ViewModels;

namespace StockTakingApp.Controllers;

[Authorize(Roles = "Admin")]
public class StockController : Controller
{
    private readonly AppDbContext _context;

    public StockController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? locationId)
    {
        ViewData["Title"] = "Stock Levels";

        var locations = await _context.Locations
            .OrderBy(l => l.Code)
            .Select(l => new LocationViewModel
            {
                Id = l.Id,
                Code = l.Code,
                Name = l.Name
            })
            .ToListAsync();

        var query = _context.Stocks
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
            .Select(s => new StockViewModel
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductSku = s.Product.Sku,
                ProductName = s.Product.Name,
                ProductCategory = s.Product.Category,
                LocationId = s.LocationId,
                LocationCode = s.Location.Code,
                LocationName = s.Location.Name,
                Quantity = s.Quantity,
                UpdatedAt = s.UpdatedAt
            })
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
        var stock = await _context.Stocks
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (stock == null)
            return NotFound();

        stock.Quantity = quantity;
        stock.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_StockRow", new StockViewModel
            {
                Id = stock.Id,
                ProductId = stock.ProductId,
                ProductSku = stock.Product.Sku,
                ProductName = stock.Product.Name,
                Quantity = stock.Quantity,
                UpdatedAt = stock.UpdatedAt
            });
        }

        return RedirectToAction(nameof(Index));
    }
}
