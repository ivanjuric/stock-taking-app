using Microsoft.EntityFrameworkCore;
using StockTakingApp.Data;
using StockTakingApp.Models.Entities;
using StockTakingApp.Models.Enums;
using StockTakingApp.Models.ViewModels;
using StockTakingApp.Mapping;

namespace StockTakingApp.Services;

public sealed class StockTakingService(AppDbContext context, INotificationService notificationService) : IStockTakingService
{
    public async Task<StockTaking> CreateStockTakingAsync(
        int locationId, 
        int requestedById, 
        List<int> assignedWorkerIds, 
        string? notes)
    {
        var location = await context.Locations
            .Include(l => l.Stocks)
            .ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(l => l.Id == locationId)
            ?? throw new ArgumentException("Location not found");

        var stockTaking = new StockTaking
        {
            LocationId = locationId,
            RequestedById = requestedById,
            Status = StockTakingStatus.Requested,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        context.StockTakings.Add(stockTaking);
        await context.SaveChangesAsync();

        // Create assignments
        foreach (var workerId in assignedWorkerIds)
        {
            context.StockTakingAssignments.Add(new StockTakingAssignment
            {
                StockTakingId = stockTaking.Id,
                UserId = workerId,
                AssignedAt = DateTime.UtcNow
            });
        }

        // Create items for all products at this location
        foreach (var stock in location.Stocks)
        {
            context.StockTakingItems.Add(new StockTakingItem
            {
                StockTakingId = stockTaking.Id,
                ProductId = stock.ProductId,
                ExpectedQuantity = stock.Quantity
            });
        }

        await context.SaveChangesAsync();

        // Notify assigned workers
        var link = $"/stocktaking/perform/{stockTaking.Id}";
        await notificationService.CreateNotificationsForUsersAsync(
            assignedWorkerIds,
            "Stock Taking Requested",
            $"You have been assigned to count stock at {location.Name}",
            NotificationType.StockTakingRequested,
            link);

        return stockTaking;
    }

    public async Task<StockTaking?> GetStockTakingAsync(int id) =>
        await context.StockTakings
            .Include(st => st.Location)
            .Include(st => st.RequestedBy)
            .Include(st => st.Assignments)
            .ThenInclude(a => a.User)
            .Include(st => st.Items)
            .ThenInclude(i => i.Product)
            .Include(st => st.Items)
            .ThenInclude(i => i.CountedBy)
            .FirstOrDefaultAsync(st => st.Id == id);

    public async Task<StockTaking?> StartStockTakingAsync(int id, int userId)
    {
        var stockTaking = await context.StockTakings
            .Include(st => st.Location)
            .Include(st => st.Assignments)
            .FirstOrDefaultAsync(st => st.Id == id);

        if (stockTaking is null || stockTaking.Status != StockTakingStatus.Requested)
            return null;

        if (!stockTaking.Assignments.Any(a => a.UserId == userId))
            return null;

        stockTaking.Status = StockTakingStatus.InProgress;
        stockTaking.StartedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Notify admin
        await notificationService.CreateNotificationAsync(
            stockTaking.RequestedById,
            "Stock Taking Started",
            $"Stock taking at {stockTaking.Location.Name} has been started",
            NotificationType.StockTakingStarted,
            $"/stocktaking/details/{stockTaking.Id}");

        return stockTaking;
    }

    public async Task<bool> UpdateItemCountAsync(int itemId, int countedQuantity, int countedById, string? notes)
    {
        var item = await context.StockTakingItems
            .Include(i => i.StockTaking)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item is null)
            return false;

        if (item.StockTaking.Status != StockTakingStatus.InProgress)
            return false;

        item.CountedQuantity = countedQuantity;
        item.CountedById = countedById;
        item.CountedAt = DateTime.UtcNow;
        item.Notes = notes;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<StockTaking?> CompleteStockTakingAsync(int id)
    {
        var stockTaking = await context.StockTakings
            .Include(st => st.Location)
            .Include(st => st.Items)
            .FirstOrDefaultAsync(st => st.Id == id);

        if (stockTaking is null || stockTaking.Status != StockTakingStatus.InProgress)
            return null;

        // Check all items are counted
        if (stockTaking.Items.Any(i => !i.CountedQuantity.HasValue))
            return null;

        stockTaking.Status = StockTakingStatus.Completed;
        stockTaking.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Notify admin
        var discrepancies = stockTaking.Items.Count(i => i.CountedQuantity != i.ExpectedQuantity);
        var message = discrepancies > 0
            ? $"Stock taking at {stockTaking.Location.Name} completed with {discrepancies} discrepancies"
            : $"Stock taking at {stockTaking.Location.Name} completed with no discrepancies";

        await notificationService.CreateNotificationAsync(
            stockTaking.RequestedById,
            "Stock Taking Completed",
            message,
            NotificationType.StockTakingCompleted,
            $"/stocktaking/review/{stockTaking.Id}");

        return stockTaking;
    }

    public async Task<bool> AcceptCountsAsync(int stockTakingId)
    {
        var stockTaking = await context.StockTakings
            .Include(st => st.Items)
            .FirstOrDefaultAsync(st => st.Id == stockTakingId);

        if (stockTaking is null || stockTaking.Status != StockTakingStatus.Completed)
            return false;

        foreach (var item in stockTaking.Items.Where(i => i.CountedQuantity.HasValue))
        {
            var stock = await context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == item.ProductId && s.LocationId == stockTaking.LocationId);

            if (stock is not null)
            {
                stock.Quantity = item.CountedQuantity!.Value;
                stock.UpdatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<StockTakingListItemViewModel>> GetRecentStockTakingsAsync(int take = 10)
    {
        var stockTakings = await context.StockTakings
            .Include(st => st.Location)
            .Include(st => st.RequestedBy)
            .Include(st => st.Assignments)
            .ThenInclude(a => a.User)
            .Include(st => st.Items)
            .OrderByDescending(st => st.CreatedAt)
            .Take(take)
            .ToListAsync();

        return stockTakings.Select(st => st.ToListItemViewModel()).ToList();
    }

    public async Task<List<StockTakingListItemViewModel>> GetWorkerStockTakingsAsync(int userId)
    {
        var stockTakings = await context.StockTakings
            .Include(st => st.Location)
            .Include(st => st.RequestedBy)
            .Include(st => st.Assignments)
            .ThenInclude(a => a.User)
            .Include(st => st.Items)
            .Where(st => st.Assignments.Any(a => a.UserId == userId))
            .Where(st => st.Status != StockTakingStatus.Completed)
            .OrderByDescending(st => st.CreatedAt)
            .ToListAsync();

        return stockTakings.Select(st => st.ToListItemViewModel()).ToList();
    }

    public async Task<List<DiscrepancyAlertViewModel>> GetDiscrepancyAlertsAsync(int take = 10)
    {
        var items = await context.StockTakingItems
            .Include(i => i.Product)
            .Include(i => i.StockTaking)
            .ThenInclude(st => st.Location)
            .Where(i => i.StockTaking.Status == StockTakingStatus.Completed)
            .Where(i => i.CountedQuantity.HasValue && i.CountedQuantity != i.ExpectedQuantity)
            .OrderByDescending(i => i.CountedAt)
            .Take(take)
            .ToListAsync();

        return items.Select(i => i.ToDiscrepancyAlert()).ToList();
    }

    public async Task<bool> IsUserAssignedAsync(int stockTakingId, int userId) =>
        await context.StockTakingAssignments
            .AnyAsync(a => a.StockTakingId == stockTakingId && a.UserId == userId);
}
