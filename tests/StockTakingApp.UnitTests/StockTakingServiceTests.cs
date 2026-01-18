using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using StockTakingApp.Data;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.Enums;
using StockTakingApp.Services;

namespace StockTakingApp.UnitTests;

public sealed class StockTakingServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly StockTakingService _service;

    public StockTakingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _notificationServiceMock = new Mock<INotificationService>();
        _service = new StockTakingService(_context, _notificationServiceMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var admin = new User { Id = 1, Email = "admin@test.com", FullName = "Admin User", PasswordHash = "hash", Role = UserRole.Admin };
        var worker1 = new User { Id = 2, Email = "worker1@test.com", FullName = "Worker One", PasswordHash = "hash", Role = UserRole.Worker };
        var worker2 = new User { Id = 3, Email = "worker2@test.com", FullName = "Worker Two", PasswordHash = "hash", Role = UserRole.Worker };

        var location = new Location { Id = 1, Code = "WH-A", Name = "Warehouse A" };
        
        var product1 = new Product { Id = 1, Sku = "SKU001", Name = "Product 1", Category = "Category A" };
        var product2 = new Product { Id = 2, Sku = "SKU002", Name = "Product 2", Category = "Category A" };
        var product3 = new Product { Id = 3, Sku = "SKU003", Name = "Product 3", Category = "Category B" };

        var stock1 = new Stock { Id = 1, ProductId = 1, LocationId = 1, Quantity = 100 };
        var stock2 = new Stock { Id = 2, ProductId = 2, LocationId = 1, Quantity = 50 };
        var stock3 = new Stock { Id = 3, ProductId = 3, LocationId = 1, Quantity = 25 };

        _context.Users.AddRange(admin, worker1, worker2);
        _context.Locations.Add(location);
        _context.Products.AddRange(product1, product2, product3);
        _context.Stocks.AddRange(stock1, stock2, stock3);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateStockTakingAsync_ShouldCreateStockTakingWithItems()
    {
        // Arrange
        const int locationId = 1;
        const int requestedById = 1;
        var workerIds = new List<int> { 2, 3 };

        // Act
        var result = await _service.CreateStockTakingAsync(locationId, requestedById, workerIds, "Test notes");

        // Assert
        result.Should().NotBeNull();
        result.LocationId.Should().Be(locationId);
        result.RequestedById.Should().Be(requestedById);
        result.Status.Should().Be(StockTakingStatus.Requested);
        result.Notes.Should().Be("Test notes");

        var dbStockTaking = await _context.StockTakings
            .Include(st => st.Assignments)
            .Include(st => st.Items)
            .FirstAsync(st => st.Id == result.Id);

        dbStockTaking.Assignments.Should().HaveCount(2);
        dbStockTaking.Items.Should().HaveCount(3); // 3 products at this location
    }

    [Fact]
    public async Task CreateStockTakingAsync_ShouldSendNotificationsToWorkers()
    {
        // Arrange
        var workerIds = new List<int> { 2, 3 };

        // Act
        await _service.CreateStockTakingAsync(1, 1, workerIds, null);

        // Assert
        _notificationServiceMock.Verify(
            x => x.CreateNotificationsForUsersAsync(
                workerIds,
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationType.StockTakingRequested,
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task StartStockTakingAsync_ShouldUpdateStatusToInProgress()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);

        // Act
        var result = await _service.StartStockTakingAsync(stockTaking.Id, 2);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(StockTakingStatus.InProgress);
        result.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StartStockTakingAsync_ShouldFailIfUserNotAssigned()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);

        // Act
        var result = await _service.StartStockTakingAsync(stockTaking.Id, 3); // User 3 not assigned

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task StartStockTakingAsync_ShouldNotifyAdmin()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);
        _notificationServiceMock.Reset();

        // Act
        await _service.StartStockTakingAsync(stockTaking.Id, 2);

        // Assert
        _notificationServiceMock.Verify(
            x => x.CreateNotificationAsync(
                1, // admin id
                It.IsAny<string>(),
                It.IsAny<string>(),
                NotificationType.StockTakingStarted,
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateItemCountAsync_ShouldUpdateItemCount()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);
        await _service.StartStockTakingAsync(stockTaking.Id, 2);
        
        var item = await _context.StockTakingItems.FirstAsync(i => i.StockTakingId == stockTaking.Id);

        // Act
        var result = await _service.UpdateItemCountAsync(item.Id, 95, 2, "Slight shortage");

        // Assert
        result.Should().BeTrue();
        
        var updatedItem = await _context.StockTakingItems.FirstAsync(i => i.Id == item.Id);
        updatedItem.CountedQuantity.Should().Be(95);
        updatedItem.CountedById.Should().Be(2);
        updatedItem.Notes.Should().Be("Slight shortage");
        updatedItem.CountedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateItemCountAsync_ShouldFailIfNotInProgress()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);
        // NOT started - still in Requested status
        
        var item = await _context.StockTakingItems.FirstAsync(i => i.StockTakingId == stockTaking.Id);

        // Act
        var result = await _service.UpdateItemCountAsync(item.Id, 95, 2, null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteStockTakingAsync_ShouldUpdateStatusToCompleted()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);
        await _service.StartStockTakingAsync(stockTaking.Id, 2);
        
        // Count all items
        var items = await _context.StockTakingItems.Where(i => i.StockTakingId == stockTaking.Id).ToListAsync();
        foreach (var item in items)
        {
            await _service.UpdateItemCountAsync(item.Id, item.ExpectedQuantity, 2, null);
        }

        // Act
        var result = await _service.CompleteStockTakingAsync(stockTaking.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(StockTakingStatus.Completed);
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteStockTakingAsync_ShouldFailIfNotAllItemsCounted()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);
        await _service.StartStockTakingAsync(stockTaking.Id, 2);
        
        // Only count one item
        var item = await _context.StockTakingItems.FirstAsync(i => i.StockTakingId == stockTaking.Id);
        await _service.UpdateItemCountAsync(item.Id, 100, 2, null);

        // Act
        var result = await _service.CompleteStockTakingAsync(stockTaking.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CompleteStockTakingAsync_ShouldNotifyAdminWithDiscrepancyCount()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);
        await _service.StartStockTakingAsync(stockTaking.Id, 2);
        
        var items = await _context.StockTakingItems.Where(i => i.StockTakingId == stockTaking.Id).ToListAsync();
        foreach (var item in items)
        {
            // Count with some discrepancies
            await _service.UpdateItemCountAsync(item.Id, item.ExpectedQuantity - 5, 2, null);
        }
        _notificationServiceMock.Reset();

        // Act
        await _service.CompleteStockTakingAsync(stockTaking.Id);

        // Assert
        _notificationServiceMock.Verify(
            x => x.CreateNotificationAsync(
                1,
                It.IsAny<string>(),
                It.Is<string>(m => m.Contains("discrepancies")),
                NotificationType.StockTakingCompleted,
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task AcceptCountsAsync_ShouldUpdateStockQuantities()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);
        await _service.StartStockTakingAsync(stockTaking.Id, 2);
        
        var items = await _context.StockTakingItems.Where(i => i.StockTakingId == stockTaking.Id).ToListAsync();
        var newQuantities = new Dictionary<int, int>();
        foreach (var item in items)
        {
            var newQty = item.ExpectedQuantity - 10;
            newQuantities[item.ProductId] = newQty;
            await _service.UpdateItemCountAsync(item.Id, newQty, 2, null);
        }
        await _service.CompleteStockTakingAsync(stockTaking.Id);

        // Act
        var result = await _service.AcceptCountsAsync(stockTaking.Id);

        // Assert
        result.Should().BeTrue();
        
        var stocks = await _context.Stocks.Where(s => s.LocationId == 1).ToListAsync();
        foreach (var stock in stocks)
        {
            stock.Quantity.Should().Be(newQuantities[stock.ProductId]);
        }
    }

    [Fact]
    public async Task AcceptCountsAsync_ShouldFailIfNotCompleted()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);
        await _service.StartStockTakingAsync(stockTaking.Id, 2);
        // NOT completed

        // Act
        var result = await _service.AcceptCountsAsync(stockTaking.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUserAssignedAsync_ShouldReturnTrueForAssignedUser()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);

        // Act
        var result = await _service.IsUserAssignedAsync(stockTaking.Id, 2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserAssignedAsync_ShouldReturnFalseForUnassignedUser()
    {
        // Arrange
        var stockTaking = await _service.CreateStockTakingAsync(1, 1, [2], null);

        // Act
        var result = await _service.IsUserAssignedAsync(stockTaking.Id, 3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetRecentStockTakingsAsync_ShouldReturnOrderedByCreatedAtDescending()
    {
        // Arrange
        await _service.CreateStockTakingAsync(1, 1, [2], "First");
        await Task.Delay(10);
        await _service.CreateStockTakingAsync(1, 1, [2], "Second");
        await Task.Delay(10);
        await _service.CreateStockTakingAsync(1, 1, [2], "Third");

        // Act
        var result = await _service.GetRecentStockTakingsAsync(10);

        // Assert
        result.Should().HaveCount(3);
        result[0].LocationName.Should().Be("Warehouse A");
        result.Select(r => r.Id).Should().BeInDescendingOrder();
    }
}
