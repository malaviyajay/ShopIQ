namespace EC.Models
{
    public class CheckoutViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
