using MicroMentorshipAPI.Data;
using MicroMentorshipAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MicroMentorshipAPI.Services
{
    public class TokenService
    {
        private readonly AppDBContext _context;
        private readonly string _securityKey;
        private readonly double _accessTokenExpirationInMinutes;
        private readonly int _refreshTokenExpirationInDays;
        private readonly string? _issuer;
        private readonly string? _audience;

        public TokenService(IConfiguration configuration, AppDBContext context)
        {
            _context = context;
            _securityKey = GetRequiredConfigurationValue(configuration, "JwtSettings:securityKey");
            _accessTokenExpirationInMinutes =
                Convert.ToDouble(GetRequiredConfigurationValue(configuration, "JwtSettings:ExpirationInMinutes"));
            _refreshTokenExpirationInDays =
                Convert.ToInt32(GetRequiredConfigurationValue(configuration, "JwtSettings:RefreshTokenExpirationInDays"));
            _issuer = configuration["JwtSettings:Issuer"];
            _audience = configuration["JwtSettings:Audience"];
        }

        public string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName)
            };

            if (!string.IsNullOrWhiteSpace(user.Role))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_securityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationInMinutes);
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public string GenerateRefreshToken(int userID)
        {
            var tokenId = Guid.NewGuid().ToString();
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var expiryDays = _refreshTokenExpirationInDays;
            var expiryDate = DateTime.UtcNow.AddDays(expiryDays);

            var token = new RefreshToken
            {
                UserId = userID,
                TokenId = tokenId,
                RefreshUserToken = refreshToken,
            };

            _context.RefreshTokens.Add(token);
            _context.SaveChanges();

            return refreshToken;
        }
        public async Task<RefreshToken> GetRefreshToken(string refreshToken)
        {
            return _context.RefreshTokens.FirstOrDefault(rt => rt.RefreshUserToken == refreshToken);
        }

        public void RevokeRefreshToken(string refreshToken)
        {
            var token = _context.RefreshTokens.FirstOrDefault(rt => rt.RefreshUserToken == refreshToken);
            if (token != null)
            {
                _context.RefreshTokens.Remove(token);
                _context.SaveChanges();
            }
        }

        private static string GetRequiredConfigurationValue(IConfiguration configuration, string key)
        {
            var value = configuration[key];

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            throw new InvalidOperationException(
                $"Missing required configuration value '{key}'. " +
                "Use ASP.NET Core user secrets for local development or environment variables for deployment.");
        }
    }
}
