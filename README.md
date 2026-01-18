# Stock Taking App

A warehouse inventory management system built with ASP.NET MVC, HTMX, and SQLite. This application enables warehouse staff to perform stock counts with real-time collaboration and notifications.

## Features

- **Role-based access control**: Admin and Worker roles with different capabilities
- **Stock taking workflow**: Create, assign, perform, review, and accept stock counts
- **Real-time notifications**: Server-Sent Events (SSE) for instant updates
- **Multiple worker collaboration**: Assign multiple workers to a single stock taking
- **Partial save support**: Each item count is saved immediately via HTMX
- **Discrepancy tracking**: Automatic variance calculation and alerts
- **Modern UI**: Clean, responsive design with HTMX for smooth interactions

## Tech Stack

- **.NET 10** - Latest .NET runtime
- **ASP.NET MVC** - Server-side rendering with Razor views
- **HTMX** - Dynamic HTML updates without JavaScript frameworks
- **SQLite** - Lightweight embedded database
- **Entity Framework Core** - ORM for database access
- **Server-Sent Events (SSE)** - Real-time notification delivery

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Running the Application

```bash
cd stock-taking-app

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/StockTakingApp

# Or with hot reload for development
dotnet watch --project src/StockTakingApp
```

The application will start at `https://localhost:5001` or `http://localhost:5000`.

### Demo Accounts

The database is seeded with demo data on first run:

| Role   | Email              | Password  |
|--------|-------------------|-----------|
| Admin  | admin@demo.com    | Demo123!  |
| Worker | worker1@demo.com  | Demo123!  |
| Worker | worker2@demo.com  | Demo123!  |

## Project Structure

```
stock-taking-app/
├── src/
│   └── StockTakingApp/
│       ├── Controllers/          # MVC Controllers
│       ├── Data/                 # DbContext and Seeder
│       ├── Models/
│       │   ├── Entities/         # Database entities
│       │   ├── Enums/            # Status enums
│       │   └── ViewModels/       # View models
│       ├── Services/             # Business logic
│       ├── Views/                # Razor views
│       └── wwwroot/              # Static files (CSS)
├── tests/
│   ├── StockTakingApp.UnitTests/
│   └── StockTakingApp.IntegrationTests/
└── StockTakingApp.slnx           # Solution file
```

## Stock Taking Workflow

1. **Admin creates stock taking**
   - Selects a warehouse location
   - Assigns one or more workers
   - Workers receive real-time notifications

2. **Workers perform the count**
   - Start the stock taking (status: In Progress)
   - Count each item individually
   - Progress is saved automatically via HTMX
   - Multiple workers can collaborate

3. **Complete and review**
   - Worker marks stock taking as complete when all items are counted
   - Admin receives notification with discrepancy summary

4. **Admin accepts counts**
   - Reviews counted vs expected quantities
   - Accepts counts to update actual stock levels

## Architecture

### Controllers

| Controller | Purpose |
|------------|---------|
| `AccountController` | Authentication (login/logout) |
| `HomeController` | Role-based dashboards |
| `ProductsController` | Product CRUD (Admin) |
| `LocationsController` | Location CRUD (Admin) |
| `StockController` | Stock level management (Admin) |
| `StockTakingController` | Stock taking workflow |
| `NotificationsController` | Notification list and SSE stream |

### Services

| Service | Purpose |
|---------|---------|
| `AuthService` | Password hashing (PBKDF2) and verification |
| `StockTakingService` | Complete stock taking business logic |
| `NotificationService` | Create and manage notifications |
| `NotificationHub` | In-memory pub/sub for SSE channels |

### Real-time Notifications

Notifications use Server-Sent Events (SSE) via the HTMX SSE extension:

1. User's browser connects to `/notifications/stream`
2. Server maintains a `Channel<Notification>` per user
3. When events occur, notifications are pushed through the channel
4. HTMX automatically displays toast notifications

No polling - instant delivery with minimal overhead.

## Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test project
dotnet test tests/StockTakingApp.UnitTests
dotnet test tests/StockTakingApp.IntegrationTests
```

### Test Coverage

- **Unit Tests (33 tests)**
  - `StockTakingServiceTests` - Workflow logic
  - `NotificationServiceTests` - Notification CRUD
  - `NotificationHubTests` - SSE pub/sub

- **Integration Tests (5 tests)**
  - Complete workflow end-to-end
  - Authorization constraints
  - Multi-worker collaboration

## Configuration

### Database

The SQLite database file is created in the application directory:

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=stocktaking.db"
  }
}
```

### Authentication

Cookie-based authentication with 7-day sliding expiration:

- Login path: `/account/login`
- Logout path: `/account/logout`
- Access denied: `/account/accessdenied`

## API Endpoints (HTMX)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/stocktaking/countitem` | Save item count |
| POST | `/stock/update/{id}` | Update stock level |
| POST | `/notifications/markasread/{id}` | Mark notification read |
| GET | `/notifications/stream` | SSE notification stream |
| GET | `/notifications/unreadcount` | Get unread count |

## Contributing

1. Fork the repository
2. Create a feature branch
3. Write tests for new functionality
4. Submit a pull request

## License

MIT License
