using MicroMentorshipAPI.Models;
using MicroMentorshipAPI.Processors;
using MicroMentorshipAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace MicroMentorshipAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorizeController : ControllerBase
    {
        private readonly AuthorizeProcessor _authorizeProcessor;
        private readonly TokenService _tokenService;
        private readonly LinkedInAuthService _linkedInAuthService;

        public AuthorizeController(
            AuthorizeProcessor authorizeProcessor,
            TokenService tokenService,
            LinkedInAuthService linkedInAuthService)
        {
            _authorizeProcessor = authorizeProcessor;
            _tokenService = tokenService;
            _linkedInAuthService = linkedInAuthService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(User user)
        {
            var result = await _authorizeProcessor.Register(user);
            if (result == RegisterResult.Success)
            {
                return Ok("User Registered");
            }

            if (result == RegisterResult.UserAlreadyExists)
            {
                return Conflict("An account with this email already exists.");
            }

            return BadRequest("Could not create account.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(User user)
        {
            var dbUser = await _authorizeProcessor.Login(user);
            if (dbUser == null)
                return Unauthorized("Invalid username or password");
            var token = _tokenService.GenerateAccessToken(dbUser);
            var refreshToken = _tokenService.GenerateRefreshToken(dbUser.Id);
            return Ok(new TokenModel { Token = token, RefreshToken = refreshToken });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenModel tokenModel)
        {
            var refreshToken = await _tokenService.GetRefreshToken(tokenModel.RefreshToken);
            if (refreshToken == null)
                return Unauthorized("Invalid Token");

            var user = await _authorizeProcessor.GetUserById(refreshToken.UserId);
            if (user == null)
                return Unauthorized("Invalid Token");
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);

            _tokenService.RevokeRefreshToken(tokenModel.RefreshToken);
            return Ok(new TokenModel { Token = newAccessToken, RefreshToken = newRefreshToken });
        }

        [HttpGet("linkedin/config")]
        public IActionResult GetLinkedInConfig()
        {
            var config = _linkedInAuthService.GetClientConfig();

            if (config == null)
            {
                return NotFound("LinkedIn sign-in is not configured.");
            }

            return Ok(config);
        }

        [HttpPost("linkedin/exchange")]
        public async Task<IActionResult> ExchangeLinkedInCode(LinkedInCodeExchangeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.RedirectUri))
            {
                return BadRequest("Code and redirectUri are required.");
            }

            var config = _linkedInAuthService.GetClientConfig();
            if (config == null)
            {
                return BadRequest("LinkedIn sign-in is not configured.");
            }

            if (!string.Equals(request.RedirectUri, config.RedirectUri, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Redirect URI mismatch.");
            }

            var linkedInUser = await _linkedInAuthService.ExchangeCodeForUserInfoAsync(
                request.Code,
                request.RedirectUri);

            if (linkedInUser == null || string.IsNullOrWhiteSpace(linkedInUser.Subject))
            {
                return Unauthorized("Could not validate LinkedIn sign-in.");
            }

            var dbUser = await _authorizeProcessor.LoginWithLinkedIn(linkedInUser, request.Role);
            if (dbUser == null)
            {
                return BadRequest("Could not complete LinkedIn sign-in.");
            }

            var token = _tokenService.GenerateAccessToken(dbUser);
            var refreshToken = _tokenService.GenerateRefreshToken(dbUser.Id);

            return Ok(new TokenModel
            {
                Token = token,
                RefreshToken = refreshToken
            });
        }
    }
}
