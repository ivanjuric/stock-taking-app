namespace StockTakingApp.Models.Entities;

public class StockTakingAssignment
{
    public int Id { get; set; }
    public int StockTakingId { get; set; }
    public int UserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public StockTaking StockTaking { get; set; } = null!;
    public User User { get; set; } = null!;
}
