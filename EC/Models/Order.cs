namespace EC.Models
{
    public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string? Email { get; internal set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string PaymentId { get; set; }
    public string Status { get; set; } = "";

        public List<OrderItem> Items { get; set; } = new();

    }
    }