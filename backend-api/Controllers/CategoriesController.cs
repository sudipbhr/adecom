using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherAPI.DTOs;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    public CategoriesController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.Categories
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return Ok(new CategoryDto { Id = category.Id, Name = category.Name });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        var category = new Category { Name = dto.Name };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, new CategoryDto { Id = category.Id, Name = category.Name });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();
        category.Name = dto.Name;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkInsert(List<CreateCategoryDto> dtos)
    {
        var categories = dtos.Select(dto => new Category { Name = dto.Name }).ToList();
        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();
        return Ok(new { inserted = categories.Count });
    }

    [HttpGet("products")]
    public async Task<IActionResult> WithProducts()
    {
        var data = await _context.Categories
            .Select(c => new CategoryWithProductsDto
            {
                Id = c.Id,
                Name = c.Name,
                Products = c.Products.Select(p => new ProductSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    StockQty = p.StockQty
                }).ToList()
            })
            .ToListAsync();
        return Ok(data);
    }

    [HttpGet("count")]
    public async Task<IActionResult> Count()
        => Ok(new { totalCategories = await _context.Categories.CountAsync() });
}