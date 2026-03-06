using System.ComponentModel.DataAnnotations;

namespace EC.Models
{
    public class ForgotPasswordView
    {
        [Required(ErrorMessage = "Email required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
    }
}
