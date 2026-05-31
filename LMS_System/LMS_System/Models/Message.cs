namespace LMS_System.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation properties (Add these if they are missing)
        public virtual User? Sender { get; set; }
        public virtual User? Receiver { get; set; }
    }
}
