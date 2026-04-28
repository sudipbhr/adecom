using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WeatherAPI.DTOs;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _env;

    // Fixed cache key for the full (unfiltered) product list.
    private const string AllProductsCacheKey = "products_all";

    public ProductsController(AppDbContext context, IMemoryCache cache, IWebHostEnvironment env)
    {
        _context = context;
        _cache   = cache;
        _env     = env;
    }

    // ── GET /api/products ──────────────────────────────────────────────────
    // Optional query params: name, sku, minPrice, maxPrice, categoryId
    // The full unfiltered list is cached for 5 minutes.
    // When any filter is provided the DB is queried directly (no caching).
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter)
    {
        bool hasFilter = !string.IsNullOrWhiteSpace(filter.Name)
                      || !string.IsNullOrWhiteSpace(filter.SKU)
                      || filter.MinPrice.HasValue
                      || filter.MaxPrice.HasValue
                      || filter.CategoryId.HasValue;

        if (!hasFilter && _cache.TryGetValue(AllProductsCacheKey, out List<ProductDto>? cached)){
            Console.WriteLine("Returning from cache Memory");
            return Ok(cached);
        }

        var query = _context.Products.AsQueryable();
        Console.WriteLine("Fetching from database");
        await Task.Delay(4000); 

        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(p => p.Name.ToLower().Contains(filter.Name.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.SKU))
            query = query.Where(p => p.SKU.ToLower() == filter.SKU.ToLower());

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        var results = await query
            .Select(p => new ProductDto
            {
                Id         = p.Id,
                Name       = p.Name,
                SKU        = p.SKU,
                Price      = p.Price,
                Stock      = p.Stock,
                CategoryId = p.CategoryId,
                SupplierId = p.SupplierId,
                ImageUrl   = p.ImageUrl
            })
            .ToListAsync();

        // Only cache when there are no filters.
        if (!hasFilter)
            _cache.Set(AllProductsCacheKey, results, TimeSpan.FromMinutes(5));

        return Ok(results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        return Ok(MapToDto(product));
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

    // Accepts multipart/form-data so Swagger shows the file picker alongside the other fields.
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateProductDto dto)
    {
        var product = new Product
        {
            Name       = dto.Name,
            SKU        = dto.SKU,
            Price      = dto.Price,
            Stock      = dto.Stock,
            CategoryId = dto.CategoryId,
            SupplierId = dto.SupplierId,
            ImageUrl   = await SaveImageAsync(dto.Image)
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, MapToDto(product));
    }

    // Accepts multipart/form-data so Swagger shows the file picker alongside the other fields.
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.Name       = dto.Name;
        product.SKU        = dto.SKU;
        product.Price      = dto.Price;
        product.Stock      = dto.Stock;
        product.CategoryId = dto.CategoryId;
        product.SupplierId = dto.SupplierId;

        // Only update the image when a new file is supplied.
        // if (dto.Image != null)
        //     product.ImageUrl = await SaveImageAsync(dto.Image);

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
            Name       = dto.Name,
            SKU        = dto.SKU,
            Price      = dto.Price,
            Stock      = dto.Stock,
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
                p.Id, p.Name, p.SKU, p.Price, p.Stock, p.ImageUrl,
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
                Id = p.Id, Name = p.Name, SKU = p.SKU, Price = p.Price,
                Stock = p.Stock, CategoryId = p.CategoryId, SupplierId = p.SupplierId, ImageUrl = p.ImageUrl
            })
            .ToListAsync();
        return Ok(data);
    }

    [HttpPut("bulk-update-price")]
    public async Task<IActionResult> BulkUpdatePrice(List<BulkPriceUpdateDto> updates)
    {
        var ids      = updates.Select(x => x.ProductId).ToList();
        var products = await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

        foreach (var p in products)
        {
            var u = updates.First(x => x.ProductId == p.Id);
            p.Price = u.NewPrice;
        }

        await _context.SaveChangesAsync();
        return Ok(new { updated = products.Count });
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    // Saves an uploaded image to wwwroot/uploads/products/ and returns its relative URL.
    // Returns null when no file is provided.
    // Allowed types: jpg, jpeg, png, gif, webp — max 5 MB.
    private async Task<string?> SaveImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            throw new InvalidOperationException("Unsupported image type. Allowed: jpg, jpeg, png, gif, webp.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Image size exceeds 5 MB limit.");

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        await using var stream = System.IO.File.Create(Path.Combine(uploadDir, fileName));
        await file.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id         = p.Id,
        Name       = p.Name,
        SKU        = p.SKU,
        Price      = p.Price,
        Stock      = p.Stock,
        CategoryId = p.CategoryId,
        SupplierId = p.SupplierId,
        ImageUrl   = p.ImageUrl
    };
}

