namespace EC.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }  // decimal not int
        public int TotalAmount { get; set; }
    }


}
