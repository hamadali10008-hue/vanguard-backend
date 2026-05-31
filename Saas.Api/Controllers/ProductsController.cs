using Microsoft.AspNetCore.Mvc;
using Saas.Application.Interfaces;
using Saas.Domain.Entities;

namespace Saas.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // This makes the URL: api/products
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    // We "Inject" the service we registered in Program.cs
    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    // POST: api/products
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        var created = await _productService.CreateProductAsync(product);

        // Returns a 201 Created status
        return CreatedAtAction(nameof(GetProducts), new { id = created.Id }, created);
    }
}