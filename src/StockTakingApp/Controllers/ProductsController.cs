using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.ViewModels;

namespace StockTakingApp.Controllers;

[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? category)
    {
        ViewData["Title"] = "Products";

        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category == category);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductViewModel
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var categories = await _context.Products
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var model = new ProductListViewModel
        {
            Products = products,
            SearchTerm = search,
            CategoryFilter = category,
            Categories = categories
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Create Product";
        return View(new ProductViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await _context.Products.AnyAsync(p => p.Sku == model.Sku))
        {
            ModelState.AddModelError("Sku", "A product with this SKU already exists");
            return View(model);
        }

        var product = new Product
        {
            Sku = model.Sku,
            Name = model.Name,
            Description = model.Description,
            Category = model.Category
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["Title"] = "Edit Product";

        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        var model = new ProductViewModel
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            Category = product.Category
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        if (await _context.Products.AnyAsync(p => p.Sku == model.Sku && p.Id != id))
        {
            ModelState.AddModelError("Sku", "A product with this SKU already exists");
            return View(model);
        }

        product.Sku = model.Sku;
        product.Name = model.Name;
        product.Description = model.Description;
        product.Category = model.Category;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        if (Request.Headers.ContainsKey("HX-Request"))
            return Ok();

        return RedirectToAction(nameof(Index));
    }
}
