using System.Collections.Concurrent;
using System.Threading.Channels;
using StockTakingApp.Models.Entities;

namespace StockTakingApp.Services;

public class NotificationHub : INotificationHub
{
    private readonly ConcurrentDictionary<int, ConcurrentBag<Channel<Notification>>> _connections = new();

    public void Subscribe(int userId, Channel<Notification> channel)
    {
        var channels = _connections.GetOrAdd(userId, _ => new ConcurrentBag<Channel<Notification>>());
        channels.Add(channel);
    }

    public void Unsubscribe(int userId, Channel<Notification> channel)
    {
        if (_connections.TryGetValue(userId, out var channels))
        {
            // ConcurrentBag doesn't support removal, so we rebuild without this channel
            var remaining = new ConcurrentBag<Channel<Notification>>(
                channels.Where(c => c != channel));
            
            if (remaining.IsEmpty)
            {
                _connections.TryRemove(userId, out _);
            }
            else
            {
                _connections[userId] = remaining;
            }
        }
    }

    public async Task SendToUserAsync(int userId, Notification notification)
    {
        if (_connections.TryGetValue(userId, out var channels))
        {
            var tasks = channels.Select(async channel =>
            {
                try
                {
                    await channel.Writer.WriteAsync(notification);
                }
                catch (ChannelClosedException)
                {
                    // Channel was closed, ignore
                }
            });

            await Task.WhenAll(tasks);
        }
    }

    public async Task SendToUsersAsync(IEnumerable<int> userIds, Notification notification)
    {
        var tasks = userIds.Select(userId => SendToUserAsync(userId, notification));
        await Task.WhenAll(tasks);
    }
}
