

public class OrderItem
{
    // Composite Key Part 1
    public int OrderId { get; set; }
    public Order Order { get; set; }

    // Composite Key Part 2
    public int ProductId { get; set; }
    public Product Product { get; set; }

    // Extra fields
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}


