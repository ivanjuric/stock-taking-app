namespace StockTakingApp.Models.ViewModels;

public class StockViewModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StockListViewModel
{
    public List<StockViewModel> Stocks { get; set; } = [];
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public List<LocationViewModel> Locations { get; set; } = [];
}

public class StockUpdateViewModel
{
    public int Id { get; set; }
    public int Quantity { get; set; }
}
