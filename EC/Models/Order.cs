namespace EC.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";  
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public List<OrderItem> Items { get; set; } = new(); 
       
    }
}
