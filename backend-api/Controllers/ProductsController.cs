using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherAPI.DTOs;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    public ProductsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _context.Products
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Price = p.Price,
                Stock = p.Stock,
                CategoryId = p.CategoryId,
                SupplierId = p.SupplierId
            })
            .ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        return Ok(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Price = product.Price,
            Stock = product.Stock,
            CategoryId = product.CategoryId,
            SupplierId = product.SupplierId
        });
    }

    [HttpGet("{id:int}/supplier")]
    public async Task<IActionResult> GetSupplier(int id)
    {
        var product = await _context.Products.Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        var s = product.Supplier;
        return Ok(new SupplierDto { Id = s.Id, Name = s.Name, Email = s.Email, Phone = s.Phone });
    }

    [HttpGet("{id:int}/category")]
    public async Task<IActionResult> GetCategory(int id)
    {
        var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        var c = product.Category;
        return Ok(new CategoryDto { Id = c.Id, Name = c.Name });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            SKU = dto.SKU,
            Price = dto.Price,
            Stock = dto.Stock,
            CategoryId = dto.CategoryId,
            SupplierId = dto.SupplierId
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Price = product.Price,
            Stock = product.Stock,
            CategoryId = product.CategoryId,
            SupplierId = product.SupplierId
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.Name = dto.Name;
        product.SKU = dto.SKU;
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        product.CategoryId = dto.CategoryId;
        product.SupplierId = dto.SupplierId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkInsert(List<CreateProductDto> dtos)
    {
        var products = dtos.Select(dto => new Product
        {
            Name = dto.Name,
            SKU = dto.SKU,
            Price = dto.Price,
            Stock = dto.Stock,
            CategoryId = dto.CategoryId,
            SupplierId = dto.SupplierId
        }).ToList();
        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();
        return Ok(new { inserted = products.Count });
    }

    [HttpGet("with-details")]
    public async Task<IActionResult> WithDetails()
    {
        var data = await _context.Products
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.SKU,
                p.Price,
                p.Stock,
                Category = new CategoryDto { Id = p.Category.Id, Name = p.Category.Name },
                Supplier = new SupplierDto { Id = p.Supplier.Id, Name = p.Supplier.Name, Email = p.Supplier.Email, Phone = p.Supplier.Phone }
            })
            .ToListAsync();
        return Ok(data);
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count()
        => Ok(new { totalProducts = await _context.Products.CountAsync() });

    [HttpGet("high-price")]
    public async Task<IActionResult> HighPrice([FromQuery] decimal minPrice = 100)
    {
        var data = await _context.Products
            .Where(p => p.Price > minPrice)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Price = p.Price,
                Stock = p.Stock,
                CategoryId = p.CategoryId,
                SupplierId = p.SupplierId
            })
            .ToListAsync();
        return Ok(data);
    }

    [HttpPut("bulk-update-price")]
    public async Task<IActionResult> BulkUpdatePrice(List<BulkPriceUpdateDto> updates)
    {
        var ids = updates.Select(x => x.ProductId).ToList();
        var products = await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

        foreach (var p in products)
        {
            var u = updates.First(x => x.ProductId == p.Id);
            p.Price = u.NewPrice;
        }

        await _context.SaveChangesAsync();
        return Ok(new { updated = products.Count });
    }
}