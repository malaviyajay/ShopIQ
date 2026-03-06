namespace EC.Models
{
    public class OrderDetailViewModel
    {
        public string UserName { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    
    }
}
