using ChaosWebApp.Models;
using ChaosWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChaosWebApp.Controllers;

/// <summary>
/// Product catalogue API — browse, search and manage products.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Get a paginated, sortable and filterable list of products.</summary>
    /// <param name="page">Page number (1-based). Default: 1.</param>
    /// <param name="pageSize">Items per page. Default: 10, max: 100.</param>
    /// <param name="sortBy">Sort field: name, brand, category, price, rating, stock, addeddate.</param>
    /// <param name="ascending">Sort direction. Default: true (ascending).</param>
    /// <param name="filter">Free-text filter on name, brand, category and description.</param>
    /// <response code="200">Paginated product list.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<Product>), StatusCodes.Status200OK)]
    public ActionResult<PagedResponse<Product>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "id",
        [FromQuery] bool ascending = true,
        [FromQuery] string filter = "")
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = _productService.GetProducts(page, pageSize, sortBy, ascending, filter);
        return Ok(result);
    }

    /// <summary>Get a single product by its identifier.</summary>
    /// <param name="id">Product identifier.</param>
    /// <response code="200">Product found.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Product> GetProduct(int id)
    {
        var product = _productService.GetProductById(id);
        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        return Ok(product);
    }

    /// <summary>Generate and add random products to the catalogue.</summary>
    /// <param name="count">Number of products to generate. Accepted values: 5, 10, 100, 1000.</param>
    /// <response code="200">Products generated successfully.</response>
    /// <response code="400">Invalid count value.</response>
    [HttpPost("generate/{count:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult GenerateProducts(int count)
    {
        var allowed = new[] { 5, 10, 100, 1000 };
        if (!allowed.Contains(count))
            return BadRequest(new { message = $"Count must be one of: {string.Join(", ", allowed)}." });

        _productService.AddRandomProducts(count);
        return Ok(new
        {
            message = $"{count} product(s) added successfully.",
            totalCount = _productService.TotalCount
        });
    }

    /// <summary>Get catalogue statistics.</summary>
    /// <response code="200">Statistics.</response>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetStats()
    {
        var all = _productService.GetAllProducts();
        return Ok(new
        {
            totalProducts = all.Count,
            categories = all.GroupBy(p => p.Category).Select(g => new { category = g.Key, count = g.Count() }),
            averagePrice = all.Average(p => p.Price),
            averageRating = all.Average(p => p.Rating),
            promotionsCount = all.Count(p => p.IsPromotion),
            newArrivalsCount = all.Count(p => p.IsNew)
        });
    }
}
