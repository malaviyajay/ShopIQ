namespace EC.Models
{
    public class Review
    {
        //public int Id { get; set; }
        //public int ProductId { get; set; }
        //public int UserId { get; set; }
        //public int Rating { get; set; }
        //public string Comment { get; set; }
        //public string Status { get; set; } = "Approved"; 
        //public DateTime CreatedAt { get; set; } = DateTime.Now;

        //public Product Product { get; set; }
        //public User User { get; set; }
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public string? Name { get; set; } // <-- Add this
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
