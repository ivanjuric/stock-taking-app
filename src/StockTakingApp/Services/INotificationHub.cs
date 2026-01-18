using System.Threading.Channels;
using StockTakingApp.Models.Entities;

namespace StockTakingApp.Services;

public interface INotificationHub
{
    void Subscribe(int userId, Channel<Notification> channel);
    void Unsubscribe(int userId, Channel<Notification> channel);
    Task SendToUserAsync(int userId, Notification notification);
    Task SendToUsersAsync(IEnumerable<int> userIds, Notification notification);
}
