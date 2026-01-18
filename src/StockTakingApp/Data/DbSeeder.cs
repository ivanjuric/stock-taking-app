using StockTakingApp.Models.Entities;
using StockTakingApp.Models.Enums;
using StockTakingApp.Services;

namespace StockTakingApp.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, IAuthService authService)
    {
        if (context.Users.Any())
            return; // Already seeded

        // Create users
        var admin = new User
        {
            Email = "admin@demo.com",
            PasswordHash = authService.HashPassword("Demo123!"),
            FullName = "Alice Admin",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var worker1 = new User
        {
            Email = "worker1@demo.com",
            PasswordHash = authService.HashPassword("Demo123!"),
            FullName = "Bob Worker",
            Role = UserRole.Worker,
            CreatedAt = DateTime.UtcNow.AddDays(-25)
        };

        var worker2 = new User
        {
            Email = "worker2@demo.com",
            PasswordHash = authService.HashPassword("Demo123!"),
            FullName = "Carol Worker",
            Role = UserRole.Worker,
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };

        context.Users.AddRange(admin, worker1, worker2);
        await context.SaveChangesAsync();

        // Create locations
        var locations = new List<Location>
        {
            new() { Code = "WH-A", Name = "Warehouse A - Main Storage", Description = "Primary storage facility" },
            new() { Code = "WH-B", Name = "Warehouse B - Overflow", Description = "Secondary storage for overflow inventory" },
            new() { Code = "SF-01", Name = "Store Front - Display Area", Description = "Customer-facing display area" },
            new() { Code = "BR-01", Name = "Back Room - Reserve Stock", Description = "Reserve stock behind store front" },
            new() { Code = "RET-1", Name = "Returns Processing", Description = "Area for processing returned items" }
        };

        context.Locations.AddRange(locations);
        await context.SaveChangesAsync();

        // Create products
        var products = new List<Product>
        {
            // Electronics
            new() { Sku = "ELEC-001", Name = "Wireless Mouse", Category = "Electronics", Description = "Ergonomic wireless mouse with USB receiver" },
            new() { Sku = "ELEC-002", Name = "Keyboard Pro", Category = "Electronics", Description = "Mechanical keyboard with RGB lighting" },
            new() { Sku = "ELEC-003", Name = "USB-C Hub", Category = "Electronics", Description = "7-port USB-C hub with HDMI" },
            new() { Sku = "ELEC-004", Name = "Monitor Stand", Category = "Electronics", Description = "Adjustable monitor stand with USB ports" },
            new() { Sku = "ELEC-005", Name = "Webcam HD", Category = "Electronics", Description = "1080p HD webcam with microphone" },
            
            // Office Supplies
            new() { Sku = "OFFICE-001", Name = "Stapler Heavy Duty", Category = "Office", Description = "Heavy duty stapler for up to 100 sheets" },
            new() { Sku = "OFFICE-002", Name = "Paper Clips Box", Category = "Office", Description = "Box of 100 paper clips" },
            new() { Sku = "OFFICE-003", Name = "Notebook A5", Category = "Office", Description = "A5 lined notebook, 200 pages" },
            new() { Sku = "OFFICE-004", Name = "Pen Set", Category = "Office", Description = "Set of 12 ballpoint pens" },
            new() { Sku = "OFFICE-005", Name = "Desk Organizer", Category = "Office", Description = "Multi-compartment desk organizer" },
            
            // Tools
            new() { Sku = "TOOL-001", Name = "Screwdriver Set", Category = "Tools", Description = "32-piece precision screwdriver set" },
            new() { Sku = "TOOL-002", Name = "Measuring Tape", Category = "Tools", Description = "25ft retractable measuring tape" },
            new() { Sku = "TOOL-003", Name = "Utility Knife", Category = "Tools", Description = "Retractable utility knife with extra blades" },
            new() { Sku = "TOOL-004", Name = "Flashlight LED", Category = "Tools", Description = "Rechargeable LED flashlight" },
            new() { Sku = "TOOL-005", Name = "Wrench Set", Category = "Tools", Description = "10-piece combination wrench set" },
            
            // Packaging
            new() { Sku = "PACK-001", Name = "Cardboard Box S", Category = "Packaging", Description = "Small cardboard shipping box" },
            new() { Sku = "PACK-002", Name = "Cardboard Box M", Category = "Packaging", Description = "Medium cardboard shipping box" },
            new() { Sku = "PACK-003", Name = "Bubble Wrap Roll", Category = "Packaging", Description = "50ft bubble wrap roll" },
            new() { Sku = "PACK-004", Name = "Packing Tape", Category = "Packaging", Description = "Clear packing tape, 3-pack" },
            new() { Sku = "PACK-005", Name = "Labels Pack", Category = "Packaging", Description = "500 shipping labels" }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Create stock entries (random quantities across locations)
        var random = new Random(42); // Fixed seed for reproducibility
        var stocks = new List<Stock>();

        foreach (var location in locations)
        {
            // Each location has a subset of products
            var productSubset = products.OrderBy(_ => random.Next()).Take(random.Next(12, 20)).ToList();
            foreach (var product in productSubset)
            {
                stocks.Add(new Stock
                {
                    ProductId = product.Id,
                    LocationId = location.Id,
                    Quantity = random.Next(5, 200),
                    UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                });
            }
        }

        context.Stocks.AddRange(stocks);
        await context.SaveChangesAsync();

        // Create sample stock takings
        var warehouseA = locations.First(l => l.Code == "WH-A");
        var storeFront = locations.First(l => l.Code == "SF-01");
        var warehouseB = locations.First(l => l.Code == "WH-B");
        var backRoom = locations.First(l => l.Code == "BR-01");
        var returns = locations.First(l => l.Code == "RET-1");

        // Completed stock taking (yesterday) with discrepancies
        var completedSt1 = new StockTaking
        {
            LocationId = warehouseA.Id,
            RequestedById = admin.Id,
            Status = StockTakingStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            StartedAt = DateTime.UtcNow.AddDays(-1).AddHours(-4),
            CompletedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.StockTakings.Add(completedSt1);
        await context.SaveChangesAsync();

        context.StockTakingAssignments.Add(new StockTakingAssignment
        {
            StockTakingId = completedSt1.Id,
            UserId = worker1.Id
        });

        var warehouseAStocks = stocks.Where(s => s.LocationId == warehouseA.Id).ToList();
        foreach (var stock in warehouseAStocks)
        {
            var variance = random.Next(-10, 10);
            context.StockTakingItems.Add(new StockTakingItem
            {
                StockTakingId = completedSt1.Id,
                ProductId = stock.ProductId,
                ExpectedQuantity = stock.Quantity,
                CountedQuantity = Math.Max(0, stock.Quantity + variance),
                CountedAt = DateTime.UtcNow.AddDays(-1).AddHours(-2),
                CountedById = worker1.Id
            });
        }

        // Completed stock taking (3 days ago) - all matched
        var completedSt2 = new StockTaking
        {
            LocationId = storeFront.Id,
            RequestedById = admin.Id,
            Status = StockTakingStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-4),
            StartedAt = DateTime.UtcNow.AddDays(-3).AddHours(-3),
            CompletedAt = DateTime.UtcNow.AddDays(-3)
        };
        context.StockTakings.Add(completedSt2);
        await context.SaveChangesAsync();

        context.StockTakingAssignments.Add(new StockTakingAssignment
        {
            StockTakingId = completedSt2.Id,
            UserId = worker2.Id
        });

        var storeFrontStocks = stocks.Where(s => s.LocationId == storeFront.Id).ToList();
        foreach (var stock in storeFrontStocks)
        {
            context.StockTakingItems.Add(new StockTakingItem
            {
                StockTakingId = completedSt2.Id,
                ProductId = stock.ProductId,
                ExpectedQuantity = stock.Quantity,
                CountedQuantity = stock.Quantity, // All matched
                CountedAt = DateTime.UtcNow.AddDays(-3).AddHours(-1),
                CountedById = worker2.Id
            });
        }

        // In progress stock taking
        var inProgressSt = new StockTaking
        {
            LocationId = warehouseB.Id,
            RequestedById = admin.Id,
            Status = StockTakingStatus.InProgress,
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            StartedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.StockTakings.Add(inProgressSt);
        await context.SaveChangesAsync();

        context.StockTakingAssignments.Add(new StockTakingAssignment
        {
            StockTakingId = inProgressSt.Id,
            UserId = worker1.Id
        });

        var warehouseBStocks = stocks.Where(s => s.LocationId == warehouseB.Id).ToList();
        var countedCount = warehouseBStocks.Count / 2; // Half counted
        for (int i = 0; i < warehouseBStocks.Count; i++)
        {
            var stock = warehouseBStocks[i];
            var item = new StockTakingItem
            {
                StockTakingId = inProgressSt.Id,
                ProductId = stock.ProductId,
                ExpectedQuantity = stock.Quantity
            };

            if (i < countedCount)
            {
                item.CountedQuantity = stock.Quantity + random.Next(-5, 5);
                item.CountedAt = DateTime.UtcNow.AddMinutes(-random.Next(10, 50));
                item.CountedById = worker1.Id;
            }

            context.StockTakingItems.Add(item);
        }

        // Requested stock taking (assigned to worker2)
        var requestedSt1 = new StockTaking
        {
            LocationId = backRoom.Id,
            RequestedById = admin.Id,
            Status = StockTakingStatus.Requested,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        context.StockTakings.Add(requestedSt1);
        await context.SaveChangesAsync();

        context.StockTakingAssignments.Add(new StockTakingAssignment
        {
            StockTakingId = requestedSt1.Id,
            UserId = worker2.Id
        });

        var backRoomStocks = stocks.Where(s => s.LocationId == backRoom.Id).ToList();
        foreach (var stock in backRoomStocks)
        {
            context.StockTakingItems.Add(new StockTakingItem
            {
                StockTakingId = requestedSt1.Id,
                ProductId = stock.ProductId,
                ExpectedQuantity = stock.Quantity
            });
        }

        // Requested stock taking (assigned to both workers)
        var requestedSt2 = new StockTaking
        {
            LocationId = returns.Id,
            RequestedById = admin.Id,
            Status = StockTakingStatus.Requested,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        context.StockTakings.Add(requestedSt2);
        await context.SaveChangesAsync();

        context.StockTakingAssignments.AddRange(
            new StockTakingAssignment { StockTakingId = requestedSt2.Id, UserId = worker1.Id },
            new StockTakingAssignment { StockTakingId = requestedSt2.Id, UserId = worker2.Id }
        );

        var returnsStocks = stocks.Where(s => s.LocationId == returns.Id).ToList();
        foreach (var stock in returnsStocks)
        {
            context.StockTakingItems.Add(new StockTakingItem
            {
                StockTakingId = requestedSt2.Id,
                ProductId = stock.ProductId,
                ExpectedQuantity = stock.Quantity
            });
        }

        // Create sample notifications
        context.Notifications.AddRange(
            new Notification
            {
                UserId = worker1.Id,
                Title = "Stock Taking Requested",
                Message = $"You have been assigned to count stock at {warehouseB.Name}",
                Type = NotificationType.StockTakingRequested,
                Link = $"/stocktaking/perform/{inProgressSt.Id}",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new Notification
            {
                UserId = worker1.Id,
                Title = "Stock Taking Requested",
                Message = $"You have been assigned to count stock at {returns.Name}",
                Type = NotificationType.StockTakingRequested,
                Link = $"/stocktaking/perform/{requestedSt2.Id}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new Notification
            {
                UserId = worker2.Id,
                Title = "Stock Taking Requested",
                Message = $"You have been assigned to count stock at {backRoom.Name}",
                Type = NotificationType.StockTakingRequested,
                Link = $"/stocktaking/perform/{requestedSt1.Id}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Notification
            {
                UserId = worker2.Id,
                Title = "Stock Taking Requested",
                Message = $"You have been assigned to count stock at {returns.Name}",
                Type = NotificationType.StockTakingRequested,
                Link = $"/stocktaking/perform/{requestedSt2.Id}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new Notification
            {
                UserId = admin.Id,
                Title = "Stock Taking Started",
                Message = $"Stock taking at {warehouseB.Name} has been started",
                Type = NotificationType.StockTakingStarted,
                Link = $"/stocktaking/details/{inProgressSt.Id}",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new Notification
            {
                UserId = admin.Id,
                Title = "Stock Taking Completed",
                Message = $"Stock taking at {warehouseA.Name} completed with discrepancies",
                Type = NotificationType.StockTakingCompleted,
                Link = $"/stocktaking/review/{completedSt1.Id}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Notification
            {
                UserId = admin.Id,
                Title = "Stock Taking Completed",
                Message = $"Stock taking at {storeFront.Name} completed with no discrepancies",
                Type = NotificationType.StockTakingCompleted,
                Link = $"/stocktaking/review/{completedSt2.Id}",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        );

        await context.SaveChangesAsync();
    }
}
