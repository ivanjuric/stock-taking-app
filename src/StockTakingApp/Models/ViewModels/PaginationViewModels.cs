namespace StockTakingApp.Models.ViewModels;

/// <summary>
/// Query parameters for paginated, sortable, searchable lists
/// </summary>
public sealed class PagedQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
}

/// <summary>
/// Pagination metadata for list views
/// </summary>
public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItems { get; init; }
    public required string? Search { get; init; }
    public required string? SortBy { get; init; }
    public required bool SortDesc { get; init; }
    
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int FirstItem => TotalItems == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int LastItem => Math.Min(Page * PageSize, TotalItems);
}

/// <summary>
/// View model for pagination controls partial
/// </summary>
public sealed record PaginationViewModel
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItems { get; init; }
    public required int TotalPages { get; init; }
    public required bool HasPrevious { get; init; }
    public required bool HasNext { get; init; }
    public required int FirstItem { get; init; }
    public required int LastItem { get; init; }
    public required string BaseUrl { get; init; }
    public required string? Search { get; init; }
    public required string? SortBy { get; init; }
    public required bool SortDesc { get; init; }
    public string? ExtraQueryParams { get; init; }
    
    public static PaginationViewModel FromPagedResult<T>(PagedResult<T> result, string baseUrl, string? extraQueryParams = null) => new()
    {
        Page = result.Page,
        PageSize = result.PageSize,
        TotalItems = result.TotalItems,
        TotalPages = result.TotalPages,
        HasPrevious = result.HasPrevious,
        HasNext = result.HasNext,
        FirstItem = result.FirstItem,
        LastItem = result.LastItem,
        BaseUrl = baseUrl,
        Search = result.Search,
        SortBy = result.SortBy,
        SortDesc = result.SortDesc,
        ExtraQueryParams = extraQueryParams
    };
    
    public string BuildUrl(int page)
    {
        var queryParams = new List<string> { $"page={page}", $"pageSize={PageSize}" };
        
        if (!string.IsNullOrEmpty(Search))
            queryParams.Add($"search={Uri.EscapeDataString(Search)}");
        if (!string.IsNullOrEmpty(SortBy))
            queryParams.Add($"sortBy={Uri.EscapeDataString(SortBy)}");
        if (SortDesc)
            queryParams.Add("sortDesc=true");
        if (!string.IsNullOrEmpty(ExtraQueryParams))
            queryParams.Add(ExtraQueryParams);
            
        return $"{BaseUrl}?{string.Join("&", queryParams)}";
    }
}
