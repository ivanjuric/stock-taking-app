using StockTakingApp.Models.Enums;

namespace StockTakingApp.Models.ViewModels;

public sealed record NotificationViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Link { get; init; }
    public NotificationType Type { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public string TimeAgo { get; init; } = string.Empty;
    
    public string IconClass => Type switch
    {
        NotificationType.StockTakingRequested => "icon-clipboard",
        NotificationType.StockTakingStarted => "icon-play",
        NotificationType.StockTakingCompleted => "icon-check",
        _ => "icon-info"
    };
}

public sealed record NotificationListViewModel(
    List<NotificationViewModel> Notifications,
    int UnreadCount
)
{
    public NotificationListViewModel() : this([], 0) { }
}
