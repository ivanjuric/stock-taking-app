namespace StockTakingApp.Models.Entities;

public class Location
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Stock> Stocks { get; set; } = [];
    public ICollection<StockTaking> StockTakings { get; set; } = [];
    public ICollection<LocationPhoto> Photos { get; set; } = [];
}
