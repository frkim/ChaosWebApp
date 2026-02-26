using ChaosWebApp.Models;
using ChaosWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChaosWebApp.Pages;

public class IndexModel : PageModel
{
    private readonly IProductService _productService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IProductService productService, ILogger<IndexModel> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public IEnumerable<Product> Products { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int FilteredCount { get; private set; }
    public int CurrentPage { get; private set; }
    public int PageSize { get; private set; }
    public int TotalPages { get; private set; }
    public string SortBy { get; private set; } = "id";
    public bool Ascending { get; private set; } = true;
    public string Filter { get; private set; } = string.Empty;

    public void OnGet(
        int page = 1,
        int pageSize = 10,
        string sortBy = "id",
        bool ascending = true,
        string filter = "",
        int? addCount = null)
    {
        if (addCount.HasValue && new[] { 5, 10, 100, 1000 }.Contains(addCount.Value))
        {
            _productService.AddRandomProducts(addCount.Value);
            _logger.LogInformation("Added {Count} random products", addCount.Value);
            // PRG redirect to preserve sort/filter state
            RedirectToPage(new { page, pageSize, sortBy, ascending, filter });
            return;
        }

        SortBy = sortBy;
        Ascending = ascending;
        Filter = filter ?? string.Empty;
        PageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;
        TotalCount = _productService.TotalCount;

        var result = _productService.GetProducts(page, PageSize, SortBy, Ascending, Filter);
        Products = result.Items;
        FilteredCount = result.TotalCount;
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
    }
}
