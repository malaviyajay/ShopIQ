using System.ComponentModel.DataAnnotations;

namespace EC.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? Image { get; set; }
        public int CategoryId { get; set; }
        public int Quantity { get; set; }   // ✅ IMPORTANT
        public string? Category { get; set; }
    }
}