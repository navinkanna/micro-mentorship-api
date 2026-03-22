using MicroMentorshipAPI.Data;
using MicroMentorshipAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroMentorshipAPI.Processors
{
    public class ChatFeedbackProcessor
    {
        private readonly AppDBContext _context;

        public ChatFeedbackProcessor(AppDBContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> SubmitFeedbackAsync(string userName, SubmitChatFeedbackModel model)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return (false, "User was not found.");
            }

            var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (reviewer == null)
            {
                return (false, "User was not found.");
            }

            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == model.SessionId);
            if (session == null || session.Status != "ended")
            {
                return (false, "That conversation is not available for feedback.");
            }

            var isReviewerParticipant = session.MentorUserId == reviewer.Id || session.MenteeUserId == reviewer.Id;
            if (!isReviewerParticipant)
            {
                return (false, "You can only leave feedback for your own conversations.");
            }

            var existingFeedback = await _context.ChatFeedback.FirstOrDefaultAsync(f =>
                f.ChatSessionId == model.SessionId &&
                f.ReviewerUserId == reviewer.Id);

            if (existingFeedback != null)
            {
                return (false, "Feedback has already been submitted for this conversation.");
            }

            var revieweeUserId = session.MentorUserId == reviewer.Id ? session.MenteeUserId : session.MentorUserId;
            var revieweeProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == revieweeUserId);

            if (revieweeProfile == null)
            {
                return (false, "The other member profile could not be found.");
            }

            var feedback = new ChatFeedback
            {
                ChatSessionId = model.SessionId,
                ReviewerUserId = reviewer.Id,
                RevieweeUserId = revieweeUserId,
                WasHelpful = model.WasHelpful,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.ChatFeedback.Add(feedback);

            if (model.WasHelpful)
            {
                revieweeProfile.HelpfulFeedbackCount += 1;
            }

            await _context.SaveChangesAsync();
            return (true, "Feedback saved.");
        }
    }
}
