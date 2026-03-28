public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }

    // One Category has many Products (1-to-M)
    public ICollection<Product> Products { get; set; }
}
