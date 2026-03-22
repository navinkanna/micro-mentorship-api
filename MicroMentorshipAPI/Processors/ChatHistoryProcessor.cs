using MicroMentorshipAPI.Data;
using MicroMentorshipAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroMentorshipAPI.Processors
{
    public class ChatHistoryProcessor
    {
        private readonly AppDBContext _context;

        public ChatHistoryProcessor(AppDBContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<RecentChatSummaryModel>> GetRecentChatsForUser(string userName, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return [];
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return [];
            }

            var sessions = await _context.ChatSessions
                .Where(s =>
                    s.Status == "ended" &&
                    (s.MentorUserId == user.Id || s.MenteeUserId == user.Id))
                .OrderByDescending(s => s.EndedAtUtc ?? s.StartedAtUtc)
                .Take(limit)
                .Select(s => new
                {
                    s.Id,
                    s.MentorUserId,
                    s.MenteeUserId,
                    s.StartedAtUtc,
                    s.EndedAtUtc,
                    LastMessagePreview = s.Messages
                        .OrderByDescending(m => m.SentAtUtc)
                        .Select(m => m.Content)
                        .FirstOrDefault(),
                    LastMessageSentAtUtc = s.Messages
                        .OrderByDescending(m => m.SentAtUtc)
                        .Select(m => (DateTime?)m.SentAtUtc)
                        .FirstOrDefault(),
                    MessageCount = s.Messages.Count()
                })
                .ToListAsync();

            var partnerIds = sessions
                .Select(s => s.MentorUserId == user.Id ? s.MenteeUserId : s.MentorUserId)
                .Distinct()
                .ToList();

            var usersById = await _context.Users
                .Where(u => partnerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var profilesByUserId = await _context.Profiles
                .Where(p => partnerIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId);

            return sessions.Select(session =>
            {
                var partnerUserId = session.MentorUserId == user.Id ? session.MenteeUserId : session.MentorUserId;
                usersById.TryGetValue(partnerUserId, out var partnerUser);
                profilesByUserId.TryGetValue(partnerUserId, out var partnerProfile);

                return new RecentChatSummaryModel
                {
                    SessionId = session.Id,
                    PartnerUserId = partnerUserId,
                    PartnerUserName = partnerUser?.UserName ?? string.Empty,
                    PartnerRole = partnerProfile?.Role ?? partnerUser?.Role ?? string.Empty,
                    PartnerAvatarId = string.IsNullOrWhiteSpace(partnerProfile?.AvatarId) ? "sprout" : partnerProfile.AvatarId!,
                    PartnerFirstName = partnerProfile?.FirstName ?? string.Empty,
                    PartnerLastName = partnerProfile?.LastName ?? string.Empty,
                    PartnerHeadline = partnerProfile?.Headline ?? string.Empty,
                    LastMessagePreview = session.LastMessagePreview ?? string.Empty,
                    LastMessageSentAtUtc = session.LastMessageSentAtUtc,
                    MessageCount = session.MessageCount,
                    StartedAtUtc = session.StartedAtUtc,
                    EndedAtUtc = session.EndedAtUtc
                };
            }).ToList();
        }

        public async Task CleanupExpiredEndedChatsAsync(int limit = 10)
        {
            var endedSessions = await _context.ChatSessions
                .Where(s => s.Status == "ended")
                .Select(s => new
                {
                    s.Id,
                    s.MentorUserId,
                    s.MenteeUserId,
                    SortDate = s.EndedAtUtc ?? s.StartedAtUtc
                })
                .ToListAsync();

            if (endedSessions.Count == 0)
            {
                return;
            }

            var retainedSessionsByUser = endedSessions
                .SelectMany(session => new[]
                {
                    new { UserId = session.MentorUserId, session.Id, session.SortDate },
                    new { UserId = session.MenteeUserId, session.Id, session.SortDate }
                })
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(x => x.SortDate)
                        .Take(limit)
                        .Select(x => x.Id)
                        .ToHashSet());

            var expiredSessionIds = endedSessions
                .Where(session =>
                    !retainedSessionsByUser[session.MentorUserId].Contains(session.Id) &&
                    !retainedSessionsByUser[session.MenteeUserId].Contains(session.Id))
                .Select(session => session.Id)
                .ToList();

            if (expiredSessionIds.Count == 0)
            {
                return;
            }

            var messagesToDelete = await _context.ChatMessages
                .Where(message => expiredSessionIds.Contains(message.ChatSessionId))
                .ToListAsync();

            var sessionsToDelete = await _context.ChatSessions
                .Where(session => expiredSessionIds.Contains(session.Id))
                .ToListAsync();

            _context.ChatMessages.RemoveRange(messagesToDelete);
            _context.ChatSessions.RemoveRange(sessionsToDelete);
            await _context.SaveChangesAsync();
        }

        public async Task<RecentChatTranscriptModel?> GetTranscriptForUser(string userName, int sessionId)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                return null;
            }

            var session = await _context.ChatSessions
                .Include(s => s.Messages)
                .FirstOrDefaultAsync(s =>
                    s.Id == sessionId &&
                    s.Status == "ended" &&
                    (s.MentorUserId == user.Id || s.MenteeUserId == user.Id));

            if (session == null)
            {
                return null;
            }

            var partnerUserId = session.MentorUserId == user.Id ? session.MenteeUserId : session.MentorUserId;
            var partnerUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == partnerUserId);
            var partnerProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == partnerUserId);

            return new RecentChatTranscriptModel
            {
                SessionId = session.Id,
                PartnerUserId = partnerUserId,
                PartnerUserName = partnerUser?.UserName ?? string.Empty,
                PartnerRole = partnerProfile?.Role ?? partnerUser?.Role ?? string.Empty,
                PartnerAvatarId = string.IsNullOrWhiteSpace(partnerProfile?.AvatarId) ? "sprout" : partnerProfile.AvatarId!,
                PartnerFirstName = partnerProfile?.FirstName ?? string.Empty,
                PartnerLastName = partnerProfile?.LastName ?? string.Empty,
                PartnerHeadline = partnerProfile?.Headline ?? string.Empty,
                StartedAtUtc = session.StartedAtUtc,
                EndedAtUtc = session.EndedAtUtc,
                Messages = session.Messages
                    .OrderBy(m => m.SentAtUtc)
                    .Select(m => new TranscriptMessageModel
                    {
                        SenderUserId = m.SenderUserId,
                        Content = m.Content,
                        SentAtUtc = m.SentAtUtc
                    })
                    .ToList()
            };
        }
    }
}
