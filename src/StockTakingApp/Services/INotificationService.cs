using StockTakingApp.Models.Entities;
using StockTakingApp.Models.Enums;

namespace StockTakingApp.Services;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(int userId, string title, string message, NotificationType type, string? link = null);
    Task CreateNotificationsForUsersAsync(IEnumerable<int> userIds, string title, string message, NotificationType type, string? link = null);
    Task<List<Notification>> GetUserNotificationsAsync(int userId, int take = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
}
