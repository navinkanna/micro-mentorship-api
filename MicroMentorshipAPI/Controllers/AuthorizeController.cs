using MicroMentorshipAPI.Models;
using MicroMentorshipAPI.Processors;
using MicroMentorshipAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MicroMentorshipAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorizeController : ControllerBase
    {
        private readonly AuthorizeProcessor _authorizeProcessor;
        private readonly TokenService _tokenService;

        public AuthorizeController(AuthorizeProcessor authorizeProcessor, TokenService tokenService)
        {
            _authorizeProcessor = authorizeProcessor;
            _tokenService = tokenService;
        }
        ///<summary>
        ///Register User
        ///</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(User user)
        {
            var result = await _authorizeProcessor.Register(user);
            if (result)
                return Ok("User Registered");
            return BadRequest(result);
        }

        ///<summary>
        ///Login User
        ///</summary>
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

        ///<summary>
        ///Refresh Token
        ///</summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenModel tokenModel)
        {
            var refreshToken = await _tokenService.GetRefreshToken(tokenModel.RefreshToken);
            if(refreshToken == null)
                return Unauthorized("Invalid Token");

            var user = await _authorizeProcessor.GetUserById(refreshToken.UserId);
            if (user == null)
                return Unauthorized("Invalid Token");
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);

            _tokenService.RevokeRefreshToken(tokenModel.RefreshToken);
            return Ok(new TokenModel { Token = newAccessToken, RefreshToken = newRefreshToken });
        }
    }
}
