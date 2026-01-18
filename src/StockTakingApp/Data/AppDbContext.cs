using Microsoft.EntityFrameworkCore;
using StockTakingApp.Models.Entities;

namespace StockTakingApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockTaking> StockTakings => Set<StockTaking>();
    public DbSet<StockTakingAssignment> StockTakingAssignments => Set<StockTakingAssignment>();
    public DbSet<StockTakingItem> StockTakingItems => Set<StockTakingItem>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Role).HasConversion<string>();
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.Sku).IsUnique();
        });

        // Location configuration
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Stock configuration
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasIndex(e => new { e.ProductId, e.LocationId }).IsUnique();

            entity.HasOne(e => e.Product)
                .WithMany(p => p.Stocks)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Location)
                .WithMany(l => l.Stocks)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StockTaking configuration
        modelBuilder.Entity<StockTaking>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Location)
                .WithMany(l => l.StockTakings)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RequestedBy)
                .WithMany(u => u.RequestedStockTakings)
                .HasForeignKey(e => e.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StockTakingAssignment configuration
        modelBuilder.Entity<StockTakingAssignment>(entity =>
        {
            entity.HasIndex(e => new { e.StockTakingId, e.UserId }).IsUnique();

            entity.HasOne(e => e.StockTaking)
                .WithMany(st => st.Assignments)
                .HasForeignKey(e => e.StockTakingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.StockTakingAssignments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StockTakingItem configuration
        modelBuilder.Entity<StockTakingItem>(entity =>
        {
            entity.HasIndex(e => new { e.StockTakingId, e.ProductId }).IsUnique();

            entity.HasOne(e => e.StockTaking)
                .WithMany(st => st.Items)
                .HasForeignKey(e => e.StockTakingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.StockTakingItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CountedBy)
                .WithMany()
                .HasForeignKey(e => e.CountedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.Type).HasConversion<string>();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.IsRead });
        });
    }
}
