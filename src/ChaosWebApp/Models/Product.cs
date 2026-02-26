namespace ChaosWebApp.Models;

/// <summary>Represents a product in the catalogue.</summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int Stock { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Ean { get; set; } = string.Empty;
    public bool IsNew { get; set; }
    public bool IsPromotion { get; set; }
    public DateTime AddedDate { get; set; }
}

/// <summary>Paginated response wrapper.</summary>
public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
