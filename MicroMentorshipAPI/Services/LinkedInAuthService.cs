using System.Net.Http.Headers;
using System.Text.Json;
using MicroMentorshipAPI.Models;

namespace MicroMentorshipAPI.Services
{
    public class LinkedInAuthService
    {
        private const string LinkedInTokenEndpoint = "https://www.linkedin.com/oauth/v2/accessToken";
        private const string LinkedInUserInfoEndpoint = "https://api.linkedin.com/v2/userinfo";
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public LinkedInAuthService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public LinkedInAuthConfigModel? GetClientConfig()
        {
            var clientId = _configuration["LinkedIn:ClientId"];
            var redirectUri = _configuration["LinkedIn:RedirectUri"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
            {
                return null;
            }

            return new LinkedInAuthConfigModel
            {
                ClientId = clientId,
                RedirectUri = redirectUri,
                Scope = string.IsNullOrWhiteSpace(_configuration["LinkedIn:Scope"])
                    ? "openid profile email"
                    : _configuration["LinkedIn:Scope"]!
            };
        }

        public async Task<LinkedInUserInfoResponse?> ExchangeCodeForUserInfoAsync(string code, string redirectUri)
        {
            var clientId = _configuration["LinkedIn:ClientId"];
            var clientSecret = _configuration["LinkedIn:ClientSecret"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new InvalidOperationException(
                    "Missing LinkedIn OAuth configuration. Configure LinkedIn:ClientId and LinkedIn:ClientSecret.");
            }

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, LinkedInTokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["redirect_uri"] = redirectUri
                })
            };

            using var tokenResponse = await _httpClient.SendAsync(tokenRequest);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var tokenPayload = await DeserializeAsync<LinkedInTokenResponse>(tokenResponse);
            if (tokenPayload == null || string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
            {
                return null;
            }

            using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, LinkedInUserInfoEndpoint);
            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenPayload.AccessToken);

            using var userInfoResponse = await _httpClient.SendAsync(userInfoRequest);
            if (!userInfoResponse.IsSuccessStatusCode)
            {
                return null;
            }

            return await DeserializeAsync<LinkedInUserInfoResponse>(userInfoResponse);
        }

        private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
