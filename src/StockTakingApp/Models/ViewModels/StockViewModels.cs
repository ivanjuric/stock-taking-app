namespace StockTakingApp.Models.ViewModels;

public sealed record StockViewModel
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string ProductCategory { get; init; } = string.Empty;
    public int LocationId { get; init; }
    public string LocationCode { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record StockListViewModel
{
    public required PagedResult<StockViewModel> Stocks { get; init; }
    public required int? LocationId { get; init; }
    public required string? LocationName { get; init; }
    public required List<LocationViewModel> Locations { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

public sealed record StockUpdateViewModel(int Id, int Quantity);
