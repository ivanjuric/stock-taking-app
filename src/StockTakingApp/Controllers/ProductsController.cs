using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.ViewModels;
using StockTakingApp.Mapping;

namespace StockTakingApp.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ProductsController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? search, string? category)
    {
        ViewData["Title"] = "Products";

        var query = context.Products.AsQueryable();

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
            .Select(p => p.ToViewModel())
            .ToListAsync();

        var categories = await context.Products
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

        if (await context.Products.AnyAsync(p => p.Sku == model.Sku))
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

        context.Products.Add(product);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        ViewData["Title"] = "Edit Product";

        var product = await context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        return View(product.ToViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var product = await context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        if (await context.Products.AnyAsync(p => p.Sku == model.Sku && p.Id != id))
        {
            ModelState.AddModelError("Sku", "A product with this SKU already exists");
            return View(model);
        }

        product.UpdateFromViewModel(model);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        context.Products.Remove(product);
        await context.SaveChangesAsync();

        if (Request.Headers.ContainsKey("HX-Request"))
            return Ok();

        return RedirectToAction(nameof(Index));
    }
}
