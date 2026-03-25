namespace EC.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; } // 1-5
        public string Comment { get; set; }
        public string Status { get; set; } = "Approved"; // Auto-approved
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Product Product { get; set; }
        public User User { get; set; }

        // Optional property for Razor view
        public string UserName => User?.Name ?? "Unknown";
    }
}