namespace MicroMentorshipAPI.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChatSessionId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
        public ChatSession? ChatSession { get; set; }
    }
}
