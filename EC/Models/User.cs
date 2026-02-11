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
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = "";
        public bool IsAdmin { get; set; } = false;

    }
}
       
    

