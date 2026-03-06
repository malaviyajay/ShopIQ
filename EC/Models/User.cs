using System.ComponentModel.DataAnnotations;

namespace EC.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Phone { get; set; } = "";

        [Required]
        public string Address { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [MinLength(6)]
        public string Password { get; set; } = "";

        public string? ResetToken { get; set; }
        public DateTime? TokenExpiry { get; set; }
        public string? StripeCustomerId { get; set; }

        
        public List<UserRole> Roles { get; set; } = new();
    }
}