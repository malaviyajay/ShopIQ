namespace EC.Models
{
    public class ResetPasswordView
    {
        public string Email { get; set; }

        // For Forgot Step (kept for compatibility)
        public string GeneratedToken { get; set; }

        // For Reset Step
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
