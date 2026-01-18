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
    private const int DefaultPageSize = 20;

    public async Task<IActionResult> Index(
        string? search,
        string? category,
        string? sortBy,
        bool sortDesc = false,
        int page = 1,
        int pageSize = DefaultPageSize)
    {
        ViewData["Title"] = "Products";

        var query = context.Products.AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p => 
                p.Name.ToLower().Contains(searchLower) || 
                p.Sku.ToLower().Contains(searchLower) ||
                (p.Description != null && p.Description.ToLower().Contains(searchLower)));
        }

        // Category filter
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        // Get total count before pagination
        var totalItems = await query.CountAsync();

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "sku" => sortDesc ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku),
            "category" => sortDesc ? query.OrderByDescending(p => p.Category) : query.OrderBy(p => p.Category),
            "created" => sortDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => sortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name) // default: name
        };

        // Pagination
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductViewModel
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category,
                CreatedAt = p.CreatedAt,
                Photos = p.Photos.OrderBy(ph => ph.DisplayOrder).Select(ph => new PhotoViewModel
                {
                    Id = ph.Id,
                    Url = ph.Url,
                    Caption = ph.Caption,
                    DisplayOrder = ph.DisplayOrder
                }).ToList()
            })
            .ToListAsync();

        var categories = await context.Products
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var pagedResult = new PagedResult<ProductViewModel>
        {
            Items = products,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Search = search,
            SortBy = sortBy,
            SortDesc = sortDesc
        };

        var extraParams = !string.IsNullOrEmpty(category) ? $"category={Uri.EscapeDataString(category)}" : null;

        var model = new ProductListViewModel
        {
            Products = pagedResult,
            CategoryFilter = category,
            Categories = categories,
            Pagination = PaginationViewModel.FromPagedResult(pagedResult, "/products", extraParams)
        };

        // Return partial for HTMX requests
        if (Request.Headers.ContainsKey("HX-Request"))
            return PartialView("_ProductList", model);

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

        var product = await context.Products
            .Where(p => p.Id == id)
            .Select(p => new ProductViewModel
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category,
                CreatedAt = p.CreatedAt,
                Photos = p.Photos.OrderBy(ph => ph.DisplayOrder).Select(ph => new PhotoViewModel
                {
                    Id = ph.Id,
                    Url = ph.Url,
                    Caption = ph.Caption,
                    DisplayOrder = ph.DisplayOrder
                }).ToList()
            })
            .FirstOrDefaultAsync();
        if (product is null)
            return NotFound();

        return View(product);
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

    [HttpDelete]
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
