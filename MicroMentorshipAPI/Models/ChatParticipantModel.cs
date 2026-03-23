namespace MicroMentorshipAPI.Models
{
    public class ChatParticipantModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string AvatarId { get; set; } = "sprout";
        public string AvatarMode { get; set; } = "illustration";
        public string? ProfilePhotoUrl { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string Expertise { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Topics { get; set; } = string.Empty;
        public int HelpfulFeedbackCount { get; set; }
    }
}
