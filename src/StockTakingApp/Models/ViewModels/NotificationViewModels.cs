using StockTakingApp.Models.Enums;

namespace StockTakingApp.Models.ViewModels;

public class NotificationViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
    public string IconClass => Type switch
    {
        NotificationType.StockTakingRequested => "icon-clipboard",
        NotificationType.StockTakingStarted => "icon-play",
        NotificationType.StockTakingCompleted => "icon-check",
        _ => "icon-info"
    };
}

public class NotificationListViewModel
{
    public List<NotificationViewModel> Notifications { get; set; } = [];
    public int UnreadCount { get; set; }
}
