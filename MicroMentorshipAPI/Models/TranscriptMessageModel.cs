namespace MicroMentorshipAPI.Models
{
    public class TranscriptMessageModel
    {
        public int SenderUserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAtUtc { get; set; }
    }
}
