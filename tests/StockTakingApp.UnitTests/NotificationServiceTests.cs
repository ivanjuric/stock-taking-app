using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StockTakingApp.Data;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.Enums;
using StockTakingApp.Services;

namespace StockTakingApp.UnitTests;

public sealed class NotificationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<INotificationHub> _hubMock;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _hubMock = new Mock<INotificationHub>();
        _service = new NotificationService(_context, _hubMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User { Id = 1, Email = "user@test.com", FullName = "Test User", PasswordHash = "hash", Role = UserRole.Worker };
        _context.Users.Add(user);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateNotificationAsync_ShouldCreateAndPersistNotification()
    {
        // Arrange
        const int userId = 1;
        const string title = "Test Title";
        const string message = "Test Message";
        const NotificationType type = NotificationType.StockTakingRequested;
        const string link = "/test/link";

        // Act
        var result = await _service.CreateNotificationAsync(userId, title, message, type, link);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Title.Should().Be(title);
        result.Message.Should().Be(message);
        result.Type.Should().Be(type);
        result.Link.Should().Be(link);
        result.IsRead.Should().BeFalse();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var dbNotification = await _context.Notifications.FirstAsync(n => n.Id == result.Id);
        dbNotification.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateNotificationAsync_ShouldPushToHub()
    {
        // Arrange
        const int userId = 1;

        // Act
        await _service.CreateNotificationAsync(userId, "Title", "Message", NotificationType.StockTakingStarted);

        // Assert
        _hubMock.Verify(
            x => x.SendToUserAsync(userId, It.IsAny<Notification>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateNotificationsForUsersAsync_ShouldCreateMultipleNotifications()
    {
        // Arrange
        var user2 = new User { Id = 2, Email = "user2@test.com", FullName = "User 2", PasswordHash = "hash", Role = UserRole.Worker };
        var user3 = new User { Id = 3, Email = "user3@test.com", FullName = "User 3", PasswordHash = "hash", Role = UserRole.Worker };
        _context.Users.AddRange(user2, user3);
        await _context.SaveChangesAsync();

        var userIds = new List<int> { 1, 2, 3 };

        // Act
        await _service.CreateNotificationsForUsersAsync(userIds, "Bulk Title", "Bulk Message", NotificationType.StockTakingCompleted, "/bulk");

        // Assert
        var notifications = await _context.Notifications.ToListAsync();
        notifications.Should().HaveCount(3);
        notifications.All(n => n.Title == "Bulk Title").Should().BeTrue();
        notifications.Select(n => n.UserId).Should().BeEquivalentTo(userIds);
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldReturnOrderedNotifications()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
        {
            await _service.CreateNotificationAsync(1, $"Title {i}", $"Message {i}", NotificationType.StockTakingStarted);
            await Task.Delay(10);
        }

        // Act
        var result = await _service.GetUserNotificationsAsync(1, 10);

        // Assert
        result.Should().HaveCount(5);
        result.Select(n => n.Title).First().Should().Be("Title 5"); // Most recent first
    }

    [Fact]
    public async Task GetUserNotificationsAsync_ShouldRespectTakeLimit()
    {
        // Arrange
        for (var i = 1; i <= 10; i++)
        {
            await _service.CreateNotificationAsync(1, $"Title {i}", $"Message {i}", NotificationType.StockTakingStarted);
        }

        // Act
        var result = await _service.GetUserNotificationsAsync(1, 5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await _service.CreateNotificationAsync(1, "Title 1", "Message", NotificationType.StockTakingStarted);
        await _service.CreateNotificationAsync(1, "Title 2", "Message", NotificationType.StockTakingStarted);
        await _service.CreateNotificationAsync(1, "Title 3", "Message", NotificationType.StockTakingStarted);

        // Mark one as read
        var notification = await _context.Notifications.FirstAsync();
        notification.IsRead = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUnreadCountAsync(1);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldMarkNotificationAsRead()
    {
        // Arrange
        var notification = await _service.CreateNotificationAsync(1, "Title", "Message", NotificationType.StockTakingStarted);

        // Act
        await _service.MarkAsReadAsync(notification.Id, 1);

        // Assert
        var updated = await _context.Notifications.FirstAsync(n => n.Id == notification.Id);
        updated.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldNotMarkOtherUsersNotification()
    {
        // Arrange
        var notification = await _service.CreateNotificationAsync(1, "Title", "Message", NotificationType.StockTakingStarted);

        // Act - try to mark as read with different user id
        await _service.MarkAsReadAsync(notification.Id, 999);

        // Assert - should remain unread
        var updated = await _context.Notifications.FirstAsync(n => n.Id == notification.Id);
        updated.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkAllUserNotificationsAsRead()
    {
        // Arrange
        await _service.CreateNotificationAsync(1, "Title 1", "Message", NotificationType.StockTakingStarted);
        await _service.CreateNotificationAsync(1, "Title 2", "Message", NotificationType.StockTakingStarted);
        await _service.CreateNotificationAsync(1, "Title 3", "Message", NotificationType.StockTakingStarted);

        // Act
        await _service.MarkAllAsReadAsync(1);

        // Assert
        var notifications = await _context.Notifications.Where(n => n.UserId == 1).ToListAsync();
        notifications.All(n => n.IsRead).Should().BeTrue();
    }
}
