using StockTakingApp.Models.Enums;

namespace StockTakingApp.Models.ViewModels;

public sealed record AdminDashboardViewModel(
    int TotalProducts,
    int TotalLocations,
    int PendingStockTakings,
    int InProgressStockTakings,
    int CompletedThisWeek,
    int TotalDiscrepancies,
    List<StockTakingListItemViewModel> RecentStockTakings,
    List<DiscrepancyAlertViewModel> DiscrepancyAlerts
)
{
    public AdminDashboardViewModel() : this(0, 0, 0, 0, 0, 0, [], []) { }
}

public sealed record WorkerDashboardViewModel(
    int AssignedTasks,
    int InProgressTasks,
    int CompletedThisWeek,
    List<StockTakingListItemViewModel> MyTasks
)
{
    public WorkerDashboardViewModel() : this(0, 0, 0, []) { }
}

public sealed record StockTakingListItemViewModel
{
    public int Id { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public string LocationCode { get; init; } = string.Empty;
    public StockTakingStatus Status { get; init; }
    public string StatusDisplay { get; init; } = string.Empty;
    public string StatusClass { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string RequestedByName { get; init; } = string.Empty;
    public List<string> AssignedWorkers { get; init; } = [];
    public int TotalItems { get; init; }
    public int CountedItems { get; init; }
    public int DiscrepancyCount { get; init; }
    
    public decimal ProgressPercent => TotalItems > 0 
        ? Math.Round((decimal)CountedItems / TotalItems * 100, 0) 
        : 0;
}

public sealed record DiscrepancyAlertViewModel
{
    public int StockTakingId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSku { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public int ExpectedQuantity { get; init; }
    public int CountedQuantity { get; init; }
    public int Variance { get; init; }
    public decimal VariancePercent { get; init; }
}
