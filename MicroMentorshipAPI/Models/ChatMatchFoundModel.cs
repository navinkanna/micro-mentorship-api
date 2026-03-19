namespace MicroMentorshipAPI.Models
{
    public class ChatMatchFoundModel
    {
        public int SessionId { get; set; }
        public ChatParticipantModel Partner { get; set; } = new();
        public DateTime StartedAtUtc { get; set; }
    }
}
