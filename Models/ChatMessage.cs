namespace ayul_dayusy.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Region { get; set; } = "";
        public string Message { get; set; } = "";
        public string AuthorId { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}