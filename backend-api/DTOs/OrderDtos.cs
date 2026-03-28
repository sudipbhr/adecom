using System.ComponentModel.DataAnnotations;

namespace WeatherAPI.DTOs;

public class CreateOrderDto
{
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";

    [Required]
    public int CustomerId { get; set; }

    public List<CreateOrderLineDto>? Items { get; set; }
}

/// <summary>Used for inline items when creating/updating an order.</summary>
public class CreateOrderLineDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "UnitPrice must be greater than 0")]
    public decimal UnitPrice { get; set; }
}

public class UpdateOrderDto
{
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Pending";

    [Required]
    public int CustomerId { get; set; }

    public List<CreateOrderLineDto>? Items { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int ItemCount { get; set; }
}

public class OrderDetailDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public List<OrderItemSummaryDto> OrderItems { get; set; } = new();
}

public class OrderItemSummaryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderWithDetailsDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public CustomerDto? Customer { get; set; }
    public List<OrderItemSummaryDto> OrderItems { get; set; } = new();
}
