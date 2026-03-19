namespace MicroMentorshipAPI.Models
{
    public class ChatMessageModel
    {
        public int SessionId { get; set; }
        public int SenderUserId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAtUtc { get; set; }
    }
}
