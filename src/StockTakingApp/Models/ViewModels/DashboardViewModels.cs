using StockTakingApp.Models.Entities;
using StockTakingApp.Models.Enums;

namespace StockTakingApp.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalLocations { get; set; }
    public int PendingStockTakings { get; set; }
    public int InProgressStockTakings { get; set; }
    public int CompletedThisWeek { get; set; }
    public int TotalDiscrepancies { get; set; }
    public List<StockTakingListItemViewModel> RecentStockTakings { get; set; } = [];
    public List<DiscrepancyAlertViewModel> DiscrepancyAlerts { get; set; } = [];
}

public class WorkerDashboardViewModel
{
    public int AssignedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedThisWeek { get; set; }
    public List<StockTakingListItemViewModel> MyTasks { get; set; } = [];
}

public class StockTakingListItemViewModel
{
    public int Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public StockTakingStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public List<string> AssignedWorkers { get; set; } = [];
    public int TotalItems { get; set; }
    public int CountedItems { get; set; }
    public int DiscrepancyCount { get; set; }
    public decimal ProgressPercent => TotalItems > 0 ? Math.Round((decimal)CountedItems / TotalItems * 100, 0) : 0;
}

public class DiscrepancyAlertViewModel
{
    public int StockTakingId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int CountedQuantity { get; set; }
    public int Variance { get; set; }
    public decimal VariancePercent { get; set; }
}
