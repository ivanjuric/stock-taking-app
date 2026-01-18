using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockTakingApp.Data;
using StockTakingApp.Models.Enums;
using StockTakingApp.Services;

namespace StockTakingApp.IntegrationTests;

/// <summary>
/// Integration tests for the complete stock taking workflow.
/// These tests use the actual services with an in-memory database.
/// </summary>
public class StockTakingWorkflowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StockTakingWorkflowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteWorkflow_AdminCreatesStockTaking_WorkerCountsItems_AdminAcceptsCounts()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stockTakingService = scope.ServiceProvider.GetRequiredService<IStockTakingService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Get test data
        var admin = await context.Users.FirstAsync(u => u.Role == UserRole.Admin);
        var worker = await context.Users.FirstAsync(u => u.Role == UserRole.Worker);
        var location = await context.Locations.FirstAsync();

        // Get initial stock quantities for this location
        var initialStocks = await context.Stocks
            .Where(s => s.LocationId == location.Id)
            .ToDictionaryAsync(s => s.ProductId, s => s.Quantity);

        // Step 1: Admin creates stock taking
        var stockTaking = await stockTakingService.CreateStockTakingAsync(
            location.Id, 
            admin.Id, 
            new List<int> { worker.Id }, 
            "Integration test stock taking");

        stockTaking.Should().NotBeNull();
        stockTaking.Status.Should().Be(StockTakingStatus.Requested);

        // Verify worker received notification
        var workerNotifications = await notificationService.GetUserNotificationsAsync(worker.Id);
        workerNotifications.Should().Contain(n => n.Type == NotificationType.StockTakingRequested);

        // Step 2: Worker starts the stock taking
        var startedStockTaking = await stockTakingService.StartStockTakingAsync(stockTaking.Id, worker.Id);

        startedStockTaking.Should().NotBeNull();
        startedStockTaking!.Status.Should().Be(StockTakingStatus.InProgress);
        startedStockTaking.StartedAt.Should().NotBeNull();

        // Verify admin received notification
        var adminNotifications = await notificationService.GetUserNotificationsAsync(admin.Id);
        adminNotifications.Should().Contain(n => n.Type == NotificationType.StockTakingStarted);

        // Step 3: Worker counts all items with some discrepancies
        var items = await context.StockTakingItems
            .Where(i => i.StockTakingId == stockTaking.Id)
            .ToListAsync();

        var newQuantities = new Dictionary<int, int>();
        foreach (var item in items)
        {
            // Count with a -5 variance for each item
            var countedQty = Math.Max(0, item.ExpectedQuantity - 5);
            newQuantities[item.ProductId] = countedQty;

            var success = await stockTakingService.UpdateItemCountAsync(
                item.Id, 
                countedQty, 
                worker.Id, 
                "Counted during integration test");

            success.Should().BeTrue();
        }

        // Verify all items are counted
        var updatedItems = await context.StockTakingItems
            .Where(i => i.StockTakingId == stockTaking.Id)
            .ToListAsync();

        updatedItems.All(i => i.CountedQuantity.HasValue).Should().BeTrue();
        updatedItems.All(i => i.CountedById == worker.Id).Should().BeTrue();

        // Step 4: Worker completes the stock taking
        var completedStockTaking = await stockTakingService.CompleteStockTakingAsync(stockTaking.Id);

        completedStockTaking.Should().NotBeNull();
        completedStockTaking!.Status.Should().Be(StockTakingStatus.Completed);
        completedStockTaking.CompletedAt.Should().NotBeNull();

        // Verify admin received completion notification with discrepancy info
        adminNotifications = await notificationService.GetUserNotificationsAsync(admin.Id);
        adminNotifications.Should().Contain(n => 
            n.Type == NotificationType.StockTakingCompleted && 
            n.Message.Contains("discrepancies"));

        // Step 5: Admin accepts the counts
        var acceptSuccess = await stockTakingService.AcceptCountsAsync(stockTaking.Id);

        acceptSuccess.Should().BeTrue();

        // Verify stock quantities were updated
        var updatedStocks = await context.Stocks
            .Where(s => s.LocationId == location.Id)
            .ToListAsync();

        foreach (var stock in updatedStocks)
        {
            if (newQuantities.TryGetValue(stock.ProductId, out var expectedQty))
            {
                stock.Quantity.Should().Be(expectedQty, 
                    $"Stock for product {stock.ProductId} should be updated to counted quantity");
            }
        }
    }

    [Fact]
    public async Task Workflow_CannotCompleteWithoutCountingAllItems()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stockTakingService = scope.ServiceProvider.GetRequiredService<IStockTakingService>();

        var admin = await context.Users.FirstAsync(u => u.Role == UserRole.Admin);
        var worker = await context.Users.FirstAsync(u => u.Role == UserRole.Worker);
        var location = await context.Locations.Skip(1).FirstAsync(); // Use a different location

        // Create and start stock taking
        var stockTaking = await stockTakingService.CreateStockTakingAsync(
            location.Id, admin.Id, new List<int> { worker.Id }, null);
        await stockTakingService.StartStockTakingAsync(stockTaking.Id, worker.Id);

        // Only count first item
        var firstItem = await context.StockTakingItems
            .FirstAsync(i => i.StockTakingId == stockTaking.Id);
        await stockTakingService.UpdateItemCountAsync(firstItem.Id, 10, worker.Id, null);

        // Act - try to complete
        var result = await stockTakingService.CompleteStockTakingAsync(stockTaking.Id);

        // Assert
        result.Should().BeNull("stock taking cannot be completed until all items are counted");

        var st = await context.StockTakings.FindAsync(stockTaking.Id);
        st!.Status.Should().Be(StockTakingStatus.InProgress);
    }

    [Fact]
    public async Task Workflow_UnassignedWorkerCannotStart()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stockTakingService = scope.ServiceProvider.GetRequiredService<IStockTakingService>();

        var admin = await context.Users.FirstAsync(u => u.Role == UserRole.Admin);
        var workers = await context.Users.Where(u => u.Role == UserRole.Worker).ToListAsync();
        var worker1 = workers[0];
        var worker2 = workers.Count > 1 ? workers[1] : worker1;
        var location = await context.Locations.Skip(2).FirstAsync(); // Use another location

        // Create stock taking assigned only to worker1
        var stockTaking = await stockTakingService.CreateStockTakingAsync(
            location.Id, admin.Id, new List<int> { worker1.Id }, null);

        // Act - worker2 tries to start
        var result = await stockTakingService.StartStockTakingAsync(stockTaking.Id, worker2.Id);

        // Assert
        if (worker1.Id != worker2.Id)
        {
            result.Should().BeNull("unassigned worker should not be able to start");
        }
    }

    [Fact]
    public async Task Workflow_CannotAcceptCountsBeforeCompletion()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stockTakingService = scope.ServiceProvider.GetRequiredService<IStockTakingService>();

        var admin = await context.Users.FirstAsync(u => u.Role == UserRole.Admin);
        var worker = await context.Users.FirstAsync(u => u.Role == UserRole.Worker);
        var location = await context.Locations.Skip(3).FirstAsync(); // Use another location

        // Create and start stock taking
        var stockTaking = await stockTakingService.CreateStockTakingAsync(
            location.Id, admin.Id, new List<int> { worker.Id }, null);
        await stockTakingService.StartStockTakingAsync(stockTaking.Id, worker.Id);

        // Act - try to accept counts while in progress
        var result = await stockTakingService.AcceptCountsAsync(stockTaking.Id);

        // Assert
        result.Should().BeFalse("counts cannot be accepted before stock taking is completed");
    }

    [Fact]
    public async Task Worker_CanSaveItemCount_UpdatesQuantityAndCountedBy()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stockTakingService = scope.ServiceProvider.GetRequiredService<IStockTakingService>();

        var admin = await context.Users.FirstAsync(u => u.Role == UserRole.Admin);
        var worker = await context.Users.FirstAsync(u => u.Role == UserRole.Worker);
        var location = await context.Locations.OrderBy(l => l.Id).Skip(4).FirstOrDefaultAsync()
            ?? await context.Locations.FirstAsync();

        // Create and start stock taking
        var stockTaking = await stockTakingService.CreateStockTakingAsync(
            location.Id, admin.Id, new List<int> { worker.Id }, null);
        await stockTakingService.StartStockTakingAsync(stockTaking.Id, worker.Id);

        // Get an item to count
        var item = await context.StockTakingItems
            .FirstAsync(i => i.StockTakingId == stockTaking.Id);
        
        var countedQuantity = 42;
        var notes = "Test count by worker";

        // Act - Worker saves item count
        var success = await stockTakingService.UpdateItemCountAsync(
            item.Id, 
            countedQuantity, 
            worker.Id, 
            notes);

        // Assert
        success.Should().BeTrue("worker should be able to save item count");

        // Verify the item was updated correctly
        var updatedItem = await context.StockTakingItems
            .Include(i => i.CountedBy)
            .FirstAsync(i => i.Id == item.Id);

        updatedItem.CountedQuantity.Should().Be(countedQuantity, "counted quantity should be saved");
        updatedItem.CountedById.Should().Be(worker.Id, "counted by should be the worker");
        updatedItem.CountedBy!.FullName.Should().NotBeNullOrEmpty();
        updatedItem.Notes.Should().Be(notes, "notes should be saved");
        updatedItem.CountedAt.Should().NotBeNull("counted at timestamp should be set");
        updatedItem.CountedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Workflow_MultipleWorkersCanCollaborate()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stockTakingService = scope.ServiceProvider.GetRequiredService<IStockTakingService>();

        var admin = await context.Users.FirstAsync(u => u.Role == UserRole.Admin);
        var workers = await context.Users.Where(u => u.Role == UserRole.Worker).ToListAsync();
        
        if (workers.Count < 2)
        {
            return; // Skip test if not enough workers
        }

        var worker1 = workers[0];
        var worker2 = workers[1];
        var location = await context.Locations.Skip(4).FirstOrDefaultAsync() 
            ?? await context.Locations.FirstAsync();

        // Create stock taking assigned to both workers
        var stockTaking = await stockTakingService.CreateStockTakingAsync(
            location.Id, admin.Id, new List<int> { worker1.Id, worker2.Id }, "Team counting");

        // Worker 1 starts
        await stockTakingService.StartStockTakingAsync(stockTaking.Id, worker1.Id);

        // Get items
        var items = await context.StockTakingItems
            .Where(i => i.StockTakingId == stockTaking.Id)
            .ToListAsync();

        if (items.Count < 2)
        {
            return; // Skip if not enough items
        }

        // Worker 1 counts first half
        var firstHalf = items.Take(items.Count / 2);
        foreach (var item in firstHalf)
        {
            await stockTakingService.UpdateItemCountAsync(item.Id, item.ExpectedQuantity, worker1.Id, null);
        }

        // Worker 2 counts second half
        var secondHalf = items.Skip(items.Count / 2);
        foreach (var item in secondHalf)
        {
            await stockTakingService.UpdateItemCountAsync(item.Id, item.ExpectedQuantity, worker2.Id, null);
        }

        // Complete
        var completed = await stockTakingService.CompleteStockTakingAsync(stockTaking.Id);

        // Assert
        completed.Should().NotBeNull();
        completed!.Status.Should().Be(StockTakingStatus.Completed);

        // Verify items were counted by different workers
        var finalItems = await context.StockTakingItems
            .Where(i => i.StockTakingId == stockTaking.Id)
            .ToListAsync();

        finalItems.Select(i => i.CountedById).Distinct().Should().HaveCountGreaterThan(1,
            "items should be counted by multiple workers");
    }
}
