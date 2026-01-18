using System.ComponentModel.DataAnnotations;

namespace StockTakingApp.Models.ViewModels;

public class LocationViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Code is required")]
    [StringLength(20, ErrorMessage = "Code cannot exceed 20 characters")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public int ProductCount { get; set; }
    public int TotalStock { get; set; }
}

public class LocationListViewModel
{
    public List<LocationViewModel> Locations { get; set; } = [];
    public string? SearchTerm { get; set; }
}
