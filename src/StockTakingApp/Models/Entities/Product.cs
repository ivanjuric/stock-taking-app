namespace StockTakingApp.Models.Entities;

public class Product
{
    public int Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Stock> Stocks { get; set; } = [];
    public ICollection<StockTakingItem> StockTakingItems { get; set; } = [];
}
