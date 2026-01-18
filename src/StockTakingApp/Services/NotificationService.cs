using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.Enums;

namespace StockTakingApp.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly INotificationHub _hub;

    public NotificationService(AppDbContext context, INotificationHub hub)
    {
        _context = context;
        _hub = hub;
    }

    public async Task<Notification> CreateNotificationAsync(int userId, string title, string message, NotificationType type, string? link = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Push real-time notification
        await _hub.SendToUserAsync(userId, notification);

        return notification;
    }

    public async Task CreateNotificationsForUsersAsync(IEnumerable<int> userIds, string title, string message, NotificationType type, string? link = null)
    {
        var userIdList = userIds.ToList();
        var notifications = userIdList.Select(userId => new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Push real-time notifications
        foreach (var notification in notifications)
        {
            await _hub.SendToUserAsync(notification.UserId, notification);
        }
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int take = 20)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }
}
