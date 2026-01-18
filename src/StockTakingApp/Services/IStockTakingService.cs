using StockTakingApp.Models.Entities;
using StockTakingApp.Models.ViewModels;

namespace StockTakingApp.Services;

public interface IStockTakingService
{
    Task<StockTaking> CreateStockTakingAsync(int locationId, int requestedById, List<int> assignedWorkerIds, string? notes);
    Task<StockTaking?> GetStockTakingAsync(int id);
    Task<StockTaking?> StartStockTakingAsync(int id, int userId);
    Task<bool> UpdateItemCountAsync(int itemId, int countedQuantity, int countedById, string? notes);
    Task<StockTaking?> CompleteStockTakingAsync(int id);
    Task<bool> AcceptCountsAsync(int stockTakingId);
    Task<List<StockTakingListItemViewModel>> GetRecentStockTakingsAsync(int take = 10);
    Task<List<StockTakingListItemViewModel>> GetWorkerStockTakingsAsync(int userId);
    Task<List<DiscrepancyAlertViewModel>> GetDiscrepancyAlertsAsync(int take = 10);
    Task<bool> IsUserAssignedAsync(int stockTakingId, int userId);
}
