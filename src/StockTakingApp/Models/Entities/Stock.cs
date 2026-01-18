namespace StockTakingApp.Models.Entities;

public class Stock
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int LocationId { get; set; }
    public int Quantity { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Product Product { get; set; } = null!;
    public Location Location { get; set; } = null!;
}
