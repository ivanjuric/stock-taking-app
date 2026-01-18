namespace StockTakingApp.Models.Entities;

public class ProductPhoto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public required string Url { get; set; }
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Product Product { get; set; } = null!;
}
