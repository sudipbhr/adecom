



public class Supplier
{
   public int Id { get; set; }
   public string Name { get; set; }
   public string Email { get; set; }
   public string? Phone { get; set; }

   // One Supplier supplies many Products (1-to-M)
   public ICollection<Product> Products { get; set; }
}
