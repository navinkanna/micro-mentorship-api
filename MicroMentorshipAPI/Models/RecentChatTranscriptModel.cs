namespace MicroMentorshipAPI.Models
{
    public class RecentChatTranscriptModel
    {
        public int SessionId { get; set; }
        public int PartnerUserId { get; set; }
        public string PartnerUserName { get; set; } = string.Empty;
        public string PartnerRole { get; set; } = string.Empty;
        public string PartnerAvatarId { get; set; } = string.Empty;
        public string PartnerAvatarMode { get; set; } = "illustration";
        public string? PartnerProfilePhotoUrl { get; set; }
        public string PartnerFirstName { get; set; } = string.Empty;
        public string PartnerLastName { get; set; } = string.Empty;
        public string PartnerHeadline { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }
        public List<TranscriptMessageModel> Messages { get; set; } = [];
    }
}
