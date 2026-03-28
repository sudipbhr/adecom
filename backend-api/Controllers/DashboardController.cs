using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    public DashboardController(AppDbContext context) => _context = context;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var result = new
        {
            totalCategories = await _context.Categories.CountAsync(),
            totalSuppliers = await _context.Suppliers.CountAsync(),
            totalProducts = await _context.Products.CountAsync(),
            totalCustomers = await _context.Customers.CountAsync(),
            totalOrders = await _context.Orders.CountAsync(),
            totalOrderItems = await _context.OrderItems.CountAsync()
        };

        return Ok(result);
    }
}