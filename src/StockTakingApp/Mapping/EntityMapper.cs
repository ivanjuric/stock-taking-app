using Riok.Mapperly.Abstractions;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.Enums;
using StockTakingApp.Models.ViewModels;

#pragma warning disable RMG012 // Unmapped target member
#pragma warning disable RMG020 // Unmapped source member

namespace StockTakingApp.Mapping;

[Mapper]
public static partial class EntityMapper
{
    // Photo mappings
    public static partial PhotoViewModel ToViewModel(this ProductPhoto photo);
    public static partial PhotoViewModel ToViewModel(this LocationPhoto photo);
    
    public static List<PhotoViewModel> ToViewModels(this IEnumerable<ProductPhoto> photos) =>
        photos.OrderBy(p => p.DisplayOrder).Select(p => p.ToViewModel()).ToList();
    
    public static List<PhotoViewModel> ToViewModels(this IEnumerable<LocationPhoto> photos) =>
        photos.OrderBy(p => p.DisplayOrder).Select(p => p.ToViewModel()).ToList();

    // Product mappings
    public static partial ProductViewModel ToViewModel(this Product product);
    public static partial void UpdateFromViewModel(this Product product, ProductViewModel viewModel);
    
    public static Product ToEntity(this ProductViewModel viewModel) => new()
    {
        Id = viewModel.Id,
        Sku = viewModel.Sku,
        Name = viewModel.Name,
        Description = viewModel.Description,
        Category = viewModel.Category,
        CreatedAt = viewModel.CreatedAt
    };

    // Location mappings
    public static partial LocationViewModel ToViewModel(this Location location);
    public static partial void UpdateFromViewModel(this Location location, LocationViewModel viewModel);
    
    public static Location ToEntity(this LocationViewModel viewModel) => new()
    {
        Id = viewModel.Id,
        Code = viewModel.Code,
        Name = viewModel.Name,
        Description = viewModel.Description,
        CreatedAt = viewModel.CreatedAt
    };
    
    // Worker mapping
    public static WorkerViewModel ToWorkerViewModel(this User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email
    };

    // Stock mappings
    public static StockViewModel ToViewModel(this Stock stock) => new()
    {
        Id = stock.Id,
        ProductId = stock.ProductId,
        ProductSku = stock.Product.Sku,
        ProductName = stock.Product.Name,
        ProductCategory = stock.Product.Category,
        LocationId = stock.LocationId,
        LocationCode = stock.Location.Code,
        LocationName = stock.Location.Name,
        Quantity = stock.Quantity,
        UpdatedAt = stock.UpdatedAt
    };

    // StockTakingItem mappings
    public static StockTakingItemViewModel ToViewModel(this StockTakingItem item) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductSku = item.Product.Sku,
        ProductName = item.Product.Name,
        ProductCategory = item.Product.Category,
        ExpectedQuantity = item.ExpectedQuantity,
        CountedQuantity = item.CountedQuantity,
        CountedAt = item.CountedAt,
        CountedByName = item.CountedBy?.FullName,
        Notes = item.Notes,
        IsCounted = item.CountedQuantity.HasValue,
        Variance = item.Variance,
        VariancePercent = item.VariancePercentage
    };

    // StockTaking mappings
    public static StockTakingListItemViewModel ToListItemViewModel(this StockTaking st) => new()
    {
        Id = st.Id,
        LocationName = st.Location.Name,
        LocationCode = st.Location.Code,
        Status = st.Status,
        StatusDisplay = st.Status.ToString(),
        StatusClass = st.Status.ToStatusClass(),
        CreatedAt = st.CreatedAt,
        StartedAt = st.StartedAt,
        CompletedAt = st.CompletedAt,
        RequestedByName = st.RequestedBy.FullName,
        AssignedWorkers = st.Assignments.Select(a => a.User.FullName).ToList(),
        TotalItems = st.Items.Count,
        CountedItems = st.Items.Count(i => i.CountedQuantity.HasValue),
        DiscrepancyCount = st.Items.Count(i => i.CountedQuantity.HasValue && i.CountedQuantity != i.ExpectedQuantity)
    };

    public static StockTakingDetailsViewModel ToDetailsViewModel(this StockTaking st, bool isAdmin, bool isAssigned) => new()
    {
        Id = st.Id,
        LocationName = st.Location.Name,
        LocationCode = st.Location.Code,
        Status = st.Status,
        StatusDisplay = st.Status.ToString(),
        StatusClass = st.Status.ToStatusClass(),
        RequestedByName = st.RequestedBy.FullName,
        AssignedWorkers = st.Assignments.Select(a => a.User.FullName).ToList(),
        CreatedAt = st.CreatedAt,
        StartedAt = st.StartedAt,
        CompletedAt = st.CompletedAt,
        Notes = st.Notes,
        Items = st.Items.OrderBy(i => i.Product.Name).Select(i => i.ToViewModel()).ToList(),
        TotalItems = st.Items.Count,
        CountedItems = st.Items.Count(i => i.CountedQuantity.HasValue),
        DiscrepancyCount = st.Items.Count(i => i.CountedQuantity.HasValue && i.CountedQuantity != i.ExpectedQuantity),
        CanStart = isAssigned && st.Status == StockTakingStatus.Requested,
        CanPerform = isAssigned && (st.Status == StockTakingStatus.Requested || st.Status == StockTakingStatus.InProgress),
        CanReview = isAdmin && st.Status == StockTakingStatus.Completed,
        CanAcceptCounts = isAdmin && st.Status == StockTakingStatus.Completed
    };

    public static StockTakingPerformViewModel ToPerformViewModel(this StockTaking st) => new()
    {
        Id = st.Id,
        LocationName = st.Location.Name,
        LocationCode = st.Location.Code,
        Status = st.Status,
        Items = st.Items
            .OrderBy(i => i.Product.Category)
            .ThenBy(i => i.Product.Name)
            .Select(i => i.ToViewModel())
            .ToList(),
        TotalItems = st.Items.Count,
        CountedItems = st.Items.Count(i => i.CountedQuantity.HasValue)
    };

    public static StockTakingReviewViewModel ToReviewViewModel(this StockTaking st) => new()
    {
        Id = st.Id,
        LocationName = st.Location.Name,
        LocationCode = st.Location.Code,
        CompletedAt = st.CompletedAt,
        AssignedWorkers = st.Assignments.Select(a => a.User.FullName).ToList(),
        Items = st.Items.OrderBy(i => i.Product.Name).Select(i => i.ToViewModel()).ToList(),
        TotalItems = st.Items.Count,
        MatchedItems = st.Items.Count(i => i.CountedQuantity.HasValue && i.CountedQuantity == i.ExpectedQuantity),
        DiscrepancyCount = st.Items.Count(i => i.CountedQuantity.HasValue && i.CountedQuantity != i.ExpectedQuantity)
    };

    // Notification mappings
    public static NotificationViewModel ToViewModel(this Notification notification, string timeAgo) => new()
    {
        Id = notification.Id,
        Title = notification.Title,
        Message = notification.Message,
        Link = notification.Link,
        Type = notification.Type,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt,
        TimeAgo = timeAgo
    };

    // Discrepancy alert mapping
    public static DiscrepancyAlertViewModel ToDiscrepancyAlert(this StockTakingItem item) => new()
    {
        StockTakingId = item.StockTakingId,
        ProductName = item.Product.Name,
        ProductSku = item.Product.Sku,
        LocationName = item.StockTaking.Location.Name,
        ExpectedQuantity = item.ExpectedQuantity,
        CountedQuantity = item.CountedQuantity!.Value,
        Variance = item.CountedQuantity!.Value - item.ExpectedQuantity,
        VariancePercent = item.ExpectedQuantity > 0
            ? Math.Round((decimal)(item.CountedQuantity.Value - item.ExpectedQuantity) / item.ExpectedQuantity * 100, 1)
            : 0
    };

    // Status helpers
    public static string ToStatusClass(this StockTakingStatus status) => status switch
    {
        StockTakingStatus.Requested => "status-requested",
        StockTakingStatus.InProgress => "status-in-progress",
        StockTakingStatus.Completed => "status-completed",
        _ => ""
    };

    public static string ToTimeAgo(this DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;

        if (span.TotalMinutes < 1)
            return "just now";
        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7)
            return $"{(int)span.TotalDays}d ago";

        return dateTime.ToString("MMM dd");
    }
}
