

public class Order
{
   public int Id { get; set; }
   public DateTime OrderDate { get; set; }
   public string Status { get; set; }  // Pending, Paid, Shipped, Cancelled

   // Many Orders belong to one Customer (M-to-1)
   public int CustomerId { get; set; }
   public Customer Customer { get; set; }

   // One Order has many OrderItems (1-to-M)
   public ICollection<OrderItem> OrderItems { get; set; }
}
