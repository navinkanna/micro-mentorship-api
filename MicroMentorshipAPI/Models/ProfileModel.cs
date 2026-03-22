namespace MicroMentorshipAPI.Models
{
    public class ProfileModel
    {
        public string? AvatarId { get; set; }
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
