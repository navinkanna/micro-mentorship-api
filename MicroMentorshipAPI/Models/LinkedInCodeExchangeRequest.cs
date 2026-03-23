namespace MicroMentorshipAPI.Models
{
    public class LinkedInCodeExchangeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string? Role { get; set; }
    }
}
