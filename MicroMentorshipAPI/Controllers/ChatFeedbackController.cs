using MicroMentorshipAPI.Models;
using MicroMentorshipAPI.Processors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroMentorshipAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatFeedbackController : ControllerBase
    {
        private readonly ChatFeedbackProcessor _chatFeedbackProcessor;

        public ChatFeedbackController(ChatFeedbackProcessor chatFeedbackProcessor)
        {
            _chatFeedbackProcessor = chatFeedbackProcessor;
        }

        [HttpPost]
        public async Task<ActionResult<string>> Submit(SubmitChatFeedbackModel model)
        {
            var result = await _chatFeedbackProcessor.SubmitFeedbackAsync(User.Identity?.Name ?? string.Empty, model);
            if (!result.Success)
            {
                return Conflict(result.Message);
            }

            return Ok(result.Message);
        }
    }
}
