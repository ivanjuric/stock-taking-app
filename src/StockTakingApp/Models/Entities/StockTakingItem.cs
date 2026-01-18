namespace StockTakingApp.Models.Entities;

public class StockTakingItem
{
    public int Id { get; set; }
    public int StockTakingId { get; set; }
    public int ProductId { get; set; }
    public int ExpectedQuantity { get; set; }
    public int? CountedQuantity { get; set; }
    public DateTime? CountedAt { get; set; }
    public int? CountedById { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public StockTaking StockTaking { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public User? CountedBy { get; set; }

    // Computed properties
    public bool IsCounted => CountedQuantity.HasValue;
    public int? Variance => CountedQuantity.HasValue ? CountedQuantity.Value - ExpectedQuantity : null;
    public decimal? VariancePercentage => ExpectedQuantity > 0 && CountedQuantity.HasValue
        ? Math.Round((decimal)(CountedQuantity.Value - ExpectedQuantity) / ExpectedQuantity * 100, 2)
        : null;
}
