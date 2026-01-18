# Stock Taking App - Session State

**Last Updated:** 2026-01-18

## Project Overview
**Stock Taking App** - A warehouse inventory management system built with ASP.NET MVC + HTMX.
**Local Path:** `/Users/jura/prog/futurama/stock-taking-app`

## Completed Work

### Session 1: Modernized Codebase (Committed: b66d315)
- Added Mapperly for entity-to-viewmodel mapping
- Converted ViewModels to records where appropriate
- Used primary constructors for controllers, services, DbContext
- All 39 tests pass

### Session 2: Pagination/Search/Sort (Committed: d8cf529)

#### Files Created:
- `/src/StockTakingApp/Models/ViewModels/PaginationViewModels.cs` - Contains:
  - `PagedQuery` - Query parameters class
  - `PagedResult<T>` - Generic paged result record
  - `PaginationViewModel` - For pagination controls partial

- `/src/StockTakingApp/Views/Shared/_Pagination.cshtml` - Reusable pagination controls with HTMX

- `/src/StockTakingApp/wwwroot/css/site.css` - Added styles for:
  - `.pagination-container`, `.pagination`, `.pagination-btn`
  - `.sortable`, `.sorted`, `.sort-icon` (sortable table headers)
  - `.filter-bar`, `.search-input-wrapper`

#### Updated Controllers with Pagination/Search/Sort:

| Controller | Search Fields | Sort Columns | Filter |
|------------|---------------|--------------|--------|
| ProductsController | name, SKU | sku, name, category, created | category |
| LocationsController | code, name | code, name, products, stock | - |
| StockController | SKU, product name | location, sku, product, category, quantity, updated | locationId |
| StockTakingController | location, worker | location, status, progress, created | status |

#### Created Partial Views:
- `/src/StockTakingApp/Views/Products/_ProductList.cshtml`
- `/src/StockTakingApp/Views/Locations/_LocationList.cshtml`
- `/src/StockTakingApp/Views/Stock/_StockList.cshtml`
- `/src/StockTakingApp/Views/StockTaking/_StockTakingList.cshtml`

#### Updated ViewModels:
- `ProductListViewModel` - now uses `PagedResult<ProductViewModel>`
- `LocationListViewModel` - now uses `PagedResult<LocationViewModel>`
- `StockListViewModel` - now uses `PagedResult<StockViewModel>`
- `StockTakingIndexViewModel` - NEW, uses `PagedResult<StockTakingListItemViewModel>`

### Features Implemented:
- Debounced search (300ms delay) via HTMX
- Sortable column headers with visual indicators
- Server-side pagination with configurable page size
- URL state preservation via `hx-push-url`
- HTMX partial updates (returns partial view for XHR requests)

## Build & Test Status
- **Build:** 0 errors, 0 warnings
- **Tests:** 39 passed (33 unit + 6 integration)

## Git Status
```
Commits:
d8cf529 Add pagination, search, and sorting to all list views
b66d315 Modernize codebase with C# 12 features and Mapperly
49abdbf Initial commit: Stock Taking App with HTMX and ASP.NET MVC
```

## Key Patterns Used

### HTMX Search Pattern (debounced):
```html
<input type="text" name="search"
       hx-get="/products"
       hx-target="#list-container"
       hx-swap="innerHTML"
       hx-push-url="true"
       hx-trigger="input changed delay:300ms, search" />
```

### HTMX Sortable Column Header:
```html
<th class="sortable sorted desc"
    hx-get="/products?sortBy=name&sortDesc=false"
    hx-target="#list-container"
    hx-swap="innerHTML"
    hx-push-url="true">
    Name
    <svg class="sort-icon">...</svg>
</th>
```

### Controller Pattern:
```csharp
// Return partial for HTMX requests
if (Request.Headers.ContainsKey("HX-Request"))
    return PartialView("_ProductList", model);
return View(model);
```

## Running the App
```bash
cd /Users/jura/prog/futurama/stock-taking-app
dotnet run --project src/StockTakingApp --urls "http://localhost:5051"
```

## Demo Accounts
| Role   | Email              | Password  |
|--------|-------------------|-----------|
| Admin  | admin@demo.com    | Demo123!  |
| Worker | worker1@demo.com  | Demo123!  |

## Files to Reference for Context
1. `/src/StockTakingApp/Models/ViewModels/PaginationViewModels.cs` - Pagination models
2. `/src/StockTakingApp/Views/Shared/_Pagination.cshtml` - Pagination partial
3. `/src/StockTakingApp/Controllers/ProductsController.cs` - Example controller pattern
4. `/src/StockTakingApp/Views/Products/_ProductList.cshtml` - Example partial pattern

## Potential Future Work
- Add unit tests for pagination logic
- Add integration tests for HTMX endpoints
- Consider extracting common pagination logic to a base controller or service
- Add keyboard navigation for pagination
- Add page size selector dropdown
