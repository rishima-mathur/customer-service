namespace CustomerService.API.Models;

public class Address
{
    public int AddressId { get; set; }
    public int CustomerId { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
