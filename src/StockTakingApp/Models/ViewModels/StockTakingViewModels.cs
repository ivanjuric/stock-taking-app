using System.ComponentModel.DataAnnotations;
using StockTakingApp.Models.Enums;

namespace StockTakingApp.Models.ViewModels;

public class StockTakingCreateViewModel
{
    [Required(ErrorMessage = "Location is required")]
    public int LocationId { get; set; }

    [Required(ErrorMessage = "At least one worker must be assigned")]
    [MinLength(1, ErrorMessage = "At least one worker must be assigned")]
    public List<int> AssignedWorkerIds { get; set; } = [];

    public string? Notes { get; set; }

    // For populating dropdowns
    public List<LocationViewModel> AvailableLocations { get; set; } = [];
    public List<WorkerViewModel> AvailableWorkers { get; set; } = [];
}

public class WorkerViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class StockTakingDetailsViewModel
{
    public int Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public StockTakingStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public List<string> AssignedWorkers { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public List<StockTakingItemViewModel> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int CountedItems { get; set; }
    public int DiscrepancyCount { get; set; }
    public decimal ProgressPercent => TotalItems > 0 ? Math.Round((decimal)CountedItems / TotalItems * 100, 0) : 0;
    public bool CanStart { get; set; }
    public bool CanPerform { get; set; }
    public bool CanReview { get; set; }
    public bool CanAcceptCounts { get; set; }
}

public class StockTakingItemViewModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int? CountedQuantity { get; set; }
    public DateTime? CountedAt { get; set; }
    public string? CountedByName { get; set; }
    public string? Notes { get; set; }
    public bool IsCounted { get; set; }
    public int? Variance { get; set; }
    public decimal? VariancePercent { get; set; }
    public string VarianceClass => Variance switch
    {
        null => "",
        0 => "variance-ok",
        > 0 => "variance-over",
        < 0 => "variance-under"
    };
}

public class StockTakingPerformViewModel
{
    public int Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public StockTakingStatus Status { get; set; }
    public List<StockTakingItemViewModel> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int CountedItems { get; set; }
    public decimal ProgressPercent => TotalItems > 0 ? Math.Round((decimal)CountedItems / TotalItems * 100, 0) : 0;
}

public class CountItemViewModel
{
    public int ItemId { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater")]
    public int CountedQuantity { get; set; }

    public string? Notes { get; set; }
}

public class StockTakingReviewViewModel
{
    public int Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public List<string> AssignedWorkers { get; set; } = [];
    public List<StockTakingItemViewModel> Items { get; set; } = [];
    public int TotalItems { get; set; }
    public int MatchedItems { get; set; }
    public int DiscrepancyCount { get; set; }
    public bool HasAcceptedCounts { get; set; }
}
