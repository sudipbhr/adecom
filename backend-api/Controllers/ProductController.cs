using Microsoft.AspNetCore.Mvc;
namespace ProductAPI;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private static readonly List<Product> products = new List<Product>  
    {
        new Product { Id = 1, Name = "Laptop", Price = 999.99m, Description = "A high-performance laptop for work and play.", Category = "Electronics", ImageUrl = "https://example.com/laptop.jpg" },
        new Product { Id = 2, Name = "Smartphone", Price = 499.99m, Description = "A sleek smartphone with the latest features.", Category = "Electronics", ImageUrl = "https://example.com/smartphone.jpg" },
        new Product { Id = 3, Name = "Headphones", Price = 199.99m, Description = "Noise-cancelling headphones for immersive sound.", Category = "Audio", ImageUrl = "https://example.com/headphones.jpg" },
        new Product { Id = 4, Name = "Coffee Maker", Price = 79.99m, Description = "Brew the perfect cup of coffee every morning.", Category = "Home Appliances", ImageUrl = "https://example.com/coffeemaker.jpg" },
        new Product { Id = 5, Name = "Gaming Console", Price = 399.99m, Description = "Experience next-gen gaming with stunning graphics.", Category = "Gaming", ImageUrl = "https://example.com/console.jpg" }
    };

    [HttpGet()]
    [Route("getall")]
    public ActionResult<List<Product>> Get()
    {
        return Ok(products);
    }

    [HttpGet("{id}")]
    public ActionResult<Product> Get(int id)
    {        var product = products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {            return NotFound();
        }
        return Ok(product); 
    }

    [HttpPost]
    public ActionResult<Product> Post(Product product)
    {
        product.Id = products.Max(p => p.Id) + 1; // Auto-increment ID
        products.Add(product);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }
    [HttpPut("{id}")]
    public ActionResult Put(int id, Product product)
    {
        var existingProduct = products.FirstOrDefault(p => p.Id == id);
        if (existingProduct == null)
        {
            return NotFound();
        }
        products.Remove(existingProduct);
        product.Id = id;
        products.Add(product);
        return Ok(product);
    }
    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {        var product = products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {            return NotFound();
        }
        products.Remove(product);
        return NoContent();
    }
}
