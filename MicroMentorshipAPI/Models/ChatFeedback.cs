namespace MicroMentorshipAPI.Models
{
    public class ChatFeedback
    {
        public int Id { get; set; }
        public int ChatSessionId { get; set; }
        public int ReviewerUserId { get; set; }
        public int RevieweeUserId { get; set; }
        public bool WasHelpful { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
