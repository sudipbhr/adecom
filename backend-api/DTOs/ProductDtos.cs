using System.ComponentModel.DataAnnotations;

namespace WeatherAPI.DTOs;

/// <summary>Query parameters for filtering the product list.</summary>
public class ProductFilterDto
{
    /// <summary>Filter by partial name (case-insensitive).</summary>
    public string? Name { get; set; }

    /// <summary>Filter by exact SKU (case-insensitive).</summary>
    public string? SKU { get; set; }

    /// <summary>Minimum price (inclusive).</summary>
    public decimal? MinPrice { get; set; }

    /// <summary>Maximum price (inclusive).</summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>Filter by category ID.</summary>
    public int? CategoryId { get; set; }
}

public class CreateProductDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    public int Stock { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int SupplierId { get; set; }

    /// <summary>Optional URL to the product image.</summary>
    public string? ImageUrl { get; set; }
}

public class UpdateProductDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    public int Stock { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int SupplierId { get; set; }

    /// <summary>Optional URL to the product image.</summary>
    public string? ImageUrl { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public int SupplierId { get; set; }

    /// <summary>Relative URL to the product image (e.g. /uploads/products/abc.jpg).</summary>
    public string? ImageUrl { get; set; }
}

public class ProductSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class BulkPriceUpdateDto
{
    public int ProductId { get; set; }
    public decimal NewPrice { get; set; }
}
