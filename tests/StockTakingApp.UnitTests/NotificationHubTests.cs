using System.Threading.Channels;
using FluentAssertions;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.Enums;
using StockTakingApp.Services;

namespace StockTakingApp.UnitTests;

public sealed class NotificationHubTests
{
    private readonly NotificationHub _hub = new();

    [Fact]
    public void Subscribe_ShouldAddChannel()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<Notification>();

        // Act
        _hub.Subscribe(1, channel);

        // Assert - no exception means success
        // The subscription is verified through the SendToUserAsync test
    }

    [Fact]
    public async Task SendToUserAsync_ShouldDeliverToSubscribedChannel()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<Notification>();
        _hub.Subscribe(1, channel);

        var notification = new Notification
        {
            Id = 1,
            UserId = 1,
            Title = "Test",
            Message = "Test message",
            Type = NotificationType.StockTakingRequested
        };

        // Act
        await _hub.SendToUserAsync(1, notification);

        // Assert
        var result = await channel.Reader.ReadAsync();
        result.Should().Be(notification);
    }

    [Fact]
    public async Task SendToUserAsync_ShouldDeliverToMultipleChannels()
    {
        // Arrange
        var channel1 = Channel.CreateUnbounded<Notification>();
        var channel2 = Channel.CreateUnbounded<Notification>();
        _hub.Subscribe(1, channel1);
        _hub.Subscribe(1, channel2);

        var notification = new Notification
        {
            Id = 1,
            UserId = 1,
            Title = "Test",
            Message = "Test message",
            Type = NotificationType.StockTakingRequested
        };

        // Act
        await _hub.SendToUserAsync(1, notification);

        // Assert
        var result1 = await channel1.Reader.ReadAsync();
        var result2 = await channel2.Reader.ReadAsync();
        result1.Should().Be(notification);
        result2.Should().Be(notification);
    }

    [Fact]
    public async Task SendToUserAsync_ShouldNotFailForUnsubscribedUser()
    {
        // Arrange
        var notification = new Notification
        {
            Id = 1,
            UserId = 999,
            Title = "Test",
            Message = "Test message",
            Type = NotificationType.StockTakingRequested
        };

        // Act & Assert - should not throw
        await _hub.SendToUserAsync(999, notification);
    }

    [Fact]
    public async Task Unsubscribe_ShouldRemoveChannel()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<Notification>();
        _hub.Subscribe(1, channel);
        _hub.Unsubscribe(1, channel);

        var notification = new Notification
        {
            Id = 1,
            UserId = 1,
            Title = "Test",
            Message = "Test message",
            Type = NotificationType.StockTakingRequested
        };

        // Act
        await _hub.SendToUserAsync(1, notification);

        // Assert - channel should not receive the notification
        channel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task SendToUsersAsync_ShouldDeliverToMultipleUsers()
    {
        // Arrange
        var channel1 = Channel.CreateUnbounded<Notification>();
        var channel2 = Channel.CreateUnbounded<Notification>();
        _hub.Subscribe(1, channel1);
        _hub.Subscribe(2, channel2);

        var notification = new Notification
        {
            Id = 1,
            Title = "Bulk Test",
            Message = "Test message",
            Type = NotificationType.StockTakingCompleted
        };

        // Act
        await _hub.SendToUsersAsync([1, 2], notification);

        // Assert
        var result1 = await channel1.Reader.ReadAsync();
        var result2 = await channel2.Reader.ReadAsync();
        result1.Title.Should().Be("Bulk Test");
        result2.Title.Should().Be("Bulk Test");
    }

    [Fact]
    public async Task SendToUserAsync_ShouldHandleClosedChannel()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<Notification>();
        _hub.Subscribe(1, channel);
        channel.Writer.Complete(); // Close the channel

        var notification = new Notification
        {
            Id = 1,
            UserId = 1,
            Title = "Test",
            Message = "Test message",
            Type = NotificationType.StockTakingRequested
        };

        // Act & Assert - should not throw
        await _hub.SendToUserAsync(1, notification);
    }

    [Fact]
    public void Unsubscribe_ShouldHandleNonExistentUser()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<Notification>();

        // Act & Assert - should not throw
        _hub.Unsubscribe(999, channel);
    }

    [Fact]
    public async Task Subscribe_ShouldAllowMultipleChannelsPerUser()
    {
        // Arrange - simulating multiple browser tabs
        var channel1 = Channel.CreateUnbounded<Notification>();
        var channel2 = Channel.CreateUnbounded<Notification>();
        var channel3 = Channel.CreateUnbounded<Notification>();

        _hub.Subscribe(1, channel1);
        _hub.Subscribe(1, channel2);
        _hub.Subscribe(1, channel3);

        var notification = new Notification
        {
            Id = 1,
            UserId = 1,
            Title = "Multi-Tab Test",
            Message = "Test message",
            Type = NotificationType.StockTakingStarted
        };

        // Act
        await _hub.SendToUserAsync(1, notification);

        // Assert - all channels should receive the notification
        (await channel1.Reader.ReadAsync()).Title.Should().Be("Multi-Tab Test");
        (await channel2.Reader.ReadAsync()).Title.Should().Be("Multi-Tab Test");
        (await channel3.Reader.ReadAsync()).Title.Should().Be("Multi-Tab Test");
    }
}
