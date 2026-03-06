
using System.ComponentModel.DataAnnotations;

namespace EC.Models
{
    public class Seller
    {
        public int Id { get; set; } // Seller ID, maps to Users.Id
        [Required]
        public string Name { get; set; } // Seller name
        [Required, EmailAddress]
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }
}