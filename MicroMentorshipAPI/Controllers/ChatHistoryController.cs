using MicroMentorshipAPI.Models;
using MicroMentorshipAPI.Processors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroMentorshipAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatHistoryController : ControllerBase
    {
        private readonly ChatHistoryProcessor _chatHistoryProcessor;

        public ChatHistoryController(ChatHistoryProcessor chatHistoryProcessor)
        {
            _chatHistoryProcessor = chatHistoryProcessor;
        }

        [HttpGet("recent")]
        public async Task<ActionResult<IReadOnlyList<RecentChatSummaryModel>>> GetRecent()
        {
            var recentChats = await _chatHistoryProcessor.GetRecentChatsForUser(User.Identity?.Name ?? string.Empty);
            return Ok(recentChats);
        }

        [HttpGet("recent/{sessionId:int}")]
        public async Task<ActionResult<RecentChatTranscriptModel>> GetTranscript(int sessionId)
        {
            var transcript = await _chatHistoryProcessor.GetTranscriptForUser(
                User.Identity?.Name ?? string.Empty,
                sessionId);

            if (transcript == null)
            {
                return NotFound();
            }

            return Ok(transcript);
        }
    }
}
