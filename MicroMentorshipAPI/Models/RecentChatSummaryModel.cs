namespace MicroMentorshipAPI.Models
{
    public class RecentChatSummaryModel
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
        public string LastMessagePreview { get; set; } = string.Empty;
        public DateTime? LastMessageSentAtUtc { get; set; }
        public int MessageCount { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }
    }
}
