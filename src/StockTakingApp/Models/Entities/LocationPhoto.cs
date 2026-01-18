namespace StockTakingApp.Models.Entities;

public class LocationPhoto
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public required string Url { get; set; }
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Location Location { get; set; } = null!;
}
