using System.ComponentModel.DataAnnotations;

namespace StockTakingApp.Models.ViewModels;

public class ProductViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
    public string Sku { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Category is required")]
    public string Category { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class ProductListViewModel
{
    public List<ProductViewModel> Products { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? CategoryFilter { get; set; }
    public List<string> Categories { get; set; } = [];
}
