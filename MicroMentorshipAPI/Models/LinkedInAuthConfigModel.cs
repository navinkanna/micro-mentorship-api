namespace MicroMentorshipAPI.Models
{
    public class LinkedInAuthConfigModel
    {
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string Scope { get; set; } = "openid profile email";
    }
}
