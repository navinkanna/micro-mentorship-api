namespace MicroMentorshipAPI.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        public int MentorUserId { get; set; }
        public int MenteeUserId { get; set; }
        public string Status { get; set; } = "active";
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAtUtc { get; set; }
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
