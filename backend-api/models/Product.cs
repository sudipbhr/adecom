

public class Product
{
   public int Id { get; set; }
   public string Name { get; set; }
   public string SKU { get; set; }
   public decimal Price { get; set; }
   public int Stock { get; set; }

   // Many Products belong to one Category (M-to-1)
   public int CategoryId { get; set; }
   public Category Category { get; set; }

   // Many Products belong to one Supplier (M-to-1)
   public int SupplierId { get; set; }
   public Supplier Supplier { get; set; }

   // One Product can appear in many OrderItems (1-to-M)
   public ICollection<OrderItem> OrderItems { get; set; }
}
