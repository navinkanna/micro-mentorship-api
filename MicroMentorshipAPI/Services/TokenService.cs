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
        private readonly IConfiguration _configuration;
        private readonly AppDBContext _context;
        public TokenService(IConfiguration configuration, AppDBContext context)
        {
            _configuration = configuration;
            _context = context;
        }
        public string GenerateAccessToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:securityKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"]));
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
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

            var expiryDays =Convert.ToInt32(_configuration["JwtSettings:RefreshTokenExpirationInDays"]);
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
        public RefreshToken GetRefreshToken(string refreshToken)
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
    }
}
