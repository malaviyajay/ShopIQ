using System.ComponentModel.DataAnnotations;

namespace EC.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = "";
        [Required]
        public decimal Price { get; set; }
        public string Image { get; set; } = "";
        public int Category { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";  // For display
        public int Quantity { get; set; } = 1;          // For cart
        public string? Description { get; internal set; }
        
    }
}
