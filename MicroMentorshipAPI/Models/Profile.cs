namespace MicroMentorshipAPI.Models
{
    public class Profile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? AvatarId { get; set; }
        public string? AvatarMode { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Role { get; set; }
        public string? Expertise { get; set; }
        public string? YearsOfExperience { get; set; }
        public string? Industry { get; set; }
        public string? Company { get; set; }
        public string? Location { get; set; }
        public string? Headline { get; set; }
        public string? Bio { get; set; }
        public string? Topics { get; set; }
        public int HelpfulFeedbackCount { get; set; }
    }
}
