using ChaosWebApp.Models;
using ChaosWebApp.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChaosWebApp.Pages;

public class ProductDetailModel : PageModel
{
    private readonly IProductService _productService;

    public ProductDetailModel(IProductService productService)
    {
        _productService = productService;
    }

    public Product? Product { get; private set; }

    public void OnGet(int id)
    {
        Product = _productService.GetProductById(id);
    }
}
