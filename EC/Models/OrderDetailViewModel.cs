namespace EC.Models
{
    public class OrderDetailViewModel
    {
        public string UserName { get; set; }
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
 

    }
}
