using StockTakingApp.Models.Enums;

namespace StockTakingApp.Models.Entities;

public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<StockTaking> RequestedStockTakings { get; set; } = [];
    public ICollection<StockTakingAssignment> StockTakingAssignments { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
