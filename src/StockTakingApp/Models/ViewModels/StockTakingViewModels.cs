using System.ComponentModel.DataAnnotations;
using StockTakingApp.Models.Enums;

namespace StockTakingApp.Models.ViewModels;

/// <summary>
/// View model for the StockTaking Index page with pagination, search, and filtering
/// </summary>
public sealed record StockTakingIndexViewModel
{
    public required PagedResult<StockTakingListItemViewModel> StockTakings { get; init; }
    public required StockTakingStatus? StatusFilter { get; init; }
    public required PaginationViewModel Pagination { get; init; }
}

// Mutable for form binding
public sealed class StockTakingCreateViewModel
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

public sealed record WorkerViewModel
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public sealed record StockTakingDetailsViewModel
{
    public int Id { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public string LocationCode { get; init; } = string.Empty;
    public StockTakingStatus Status { get; init; }
    public string StatusDisplay { get; init; } = string.Empty;
    public string StatusClass { get; init; } = string.Empty;
    public string RequestedByName { get; init; } = string.Empty;
    public List<string> AssignedWorkers { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? Notes { get; init; }
    public List<StockTakingItemViewModel> Items { get; init; } = [];
    public int TotalItems { get; init; }
    public int CountedItems { get; init; }
    public int DiscrepancyCount { get; init; }
    
    public decimal ProgressPercent => TotalItems > 0 
        ? Math.Round((decimal)CountedItems / TotalItems * 100, 0) 
        : 0;
    
    public bool CanStart { get; init; }
    public bool CanPerform { get; init; }
    public bool CanReview { get; init; }
    public bool CanAcceptCounts { get; init; }
}

public sealed record StockTakingItemViewModel
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string ProductCategory { get; init; } = string.Empty;
    public int ExpectedQuantity { get; init; }
    public int? CountedQuantity { get; init; }
    public DateTime? CountedAt { get; init; }
    public string? CountedByName { get; init; }
    public string? Notes { get; init; }
    public bool IsCounted { get; init; }
    public int? Variance { get; init; }
    public decimal? VariancePercent { get; init; }
    
    public string VarianceClass => Variance switch
    {
        null => "",
        0 => "variance-ok",
        > 0 => "variance-over",
        < 0 => "variance-under"
    };
}

public sealed record StockTakingPerformViewModel
{
    public int Id { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public string LocationCode { get; init; } = string.Empty;
    public StockTakingStatus Status { get; init; }
    public List<StockTakingItemViewModel> Items { get; init; } = [];
    public int TotalItems { get; init; }
    public int CountedItems { get; init; }
    
    public decimal ProgressPercent => TotalItems > 0 
        ? Math.Round((decimal)CountedItems / TotalItems * 100, 0) 
        : 0;
}

// Mutable for form binding
public sealed class CountItemViewModel
{
    public int ItemId { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater")]
    public int CountedQuantity { get; set; }

    public string? Notes { get; set; }
}

public sealed record StockTakingReviewViewModel
{
    public int Id { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public string LocationCode { get; init; } = string.Empty;
    public DateTime? CompletedAt { get; init; }
    public List<string> AssignedWorkers { get; init; } = [];
    public List<StockTakingItemViewModel> Items { get; init; } = [];
    public int TotalItems { get; init; }
    public int MatchedItems { get; init; }
    public int DiscrepancyCount { get; init; }
    public bool HasAcceptedCounts { get; init; }
}
