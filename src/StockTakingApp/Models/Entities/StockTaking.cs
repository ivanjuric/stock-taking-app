using StockTakingApp.Models.Enums;

namespace StockTakingApp.Models.Entities;

public class StockTaking
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public StockTakingStatus Status { get; set; } = StockTakingStatus.Requested;
    public int RequestedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public Location Location { get; set; } = null!;
    public User RequestedBy { get; set; } = null!;
    public ICollection<StockTakingAssignment> Assignments { get; set; } = [];
    public ICollection<StockTakingItem> Items { get; set; } = [];
}
