


public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string? Phone { get; set; }

    // One Customer has many Orders (1-to-M)
    public ICollection<Order> Orders { get; set; }
}
