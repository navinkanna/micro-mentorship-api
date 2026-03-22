using System.Security.Claims;
using MicroMentorshipAPI.Data;
using MicroMentorshipAPI.Models;
using MicroMentorshipAPI.Processors;
using MicroMentorshipAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MicroMentorshipAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDBContext _context;
        private readonly ChatMatchService _chatMatchService;
        private readonly ChatHistoryProcessor _chatHistoryProcessor;

        public ChatHub(
            AppDBContext context,
            ChatMatchService chatMatchService,
            ChatHistoryProcessor chatHistoryProcessor)
        {
            _context = context;
            _chatMatchService = chatMatchService;
            _chatHistoryProcessor = chatHistoryProcessor;
        }

        public override async Task OnConnectedAsync()
        {
            var currentUser = await GetCurrentQueuedUser();
            _chatMatchService.RegisterConnection(currentUser.UserId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var currentUser = await GetCurrentQueuedUser();
            _chatMatchService.CancelQueue(currentUser.UserId);

            var session = _chatMatchService.EndSessionByUserId(currentUser.UserId);
            if (session != null)
            {
                await MarkSessionEnded(session.SessionId);
                await NotifySessionEnded(session, $"{currentUser.FirstNameOrFallback()} left the chat.");
            }

            _chatMatchService.RemoveConnection(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinQueue()
        {
            var currentUser = await GetCurrentQueuedUser();
            var normalizedRole = NormalizeRole(currentUser.Role);
            if (normalizedRole == null)
            {
                throw new HubException("Please set your profile role to mentor or mentee before joining chat.");
            }

            var user = currentUser with { Role = normalizedRole, ConnectionId = Context.ConnectionId };
            var matchedUser = _chatMatchService.EnqueueOrMatch(user);
            if (matchedUser == null)
            {
                await Clients.Caller.SendAsync("QueueJoined", new ChatQueueStateModel
                {
                    State = "searching",
                    Message = $"Looking for an active {GetTargetRole(user.Role)}..."
                });
                return;
            }

            var mentor = user.Role == "mentor" ? user : matchedUser;
            var mentee = user.Role == "mentee" ? user : matchedUser;
            var chatSession = new ChatSession
            {
                MentorUserId = mentor.UserId,
                MenteeUserId = mentee.UserId,
                Status = "active",
                StartedAtUtc = DateTime.UtcNow
            };

            _context.ChatSessions.Add(chatSession);
            await _context.SaveChangesAsync();

            _chatMatchService.RegisterSession(chatSession.Id, mentor, mentee);

            await Clients.Client(mentor.ConnectionId).SendAsync("MatchFound", new ChatMatchFoundModel
            {
                SessionId = chatSession.Id,
                StartedAtUtc = chatSession.StartedAtUtc,
                Partner = ToParticipantModel(mentee)
            });

            await Clients.Client(mentee.ConnectionId).SendAsync("MatchFound", new ChatMatchFoundModel
            {
                SessionId = chatSession.Id,
                StartedAtUtc = chatSession.StartedAtUtc,
                Partner = ToParticipantModel(mentor)
            });
        }

        public Task CancelQueue()
        {
            var userId = GetCurrentUserId();
            _chatMatchService.CancelQueue(userId);
            return Clients.Caller.SendAsync("QueueCancelled", new ChatQueueStateModel
            {
                State = "idle",
                Message = "Search cancelled."
            });
        }

        public async Task SendMessage(int sessionId, string content)
        {
            var trimmedContent = content?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedContent))
            {
                return;
            }

            if (trimmedContent.Length > 1000)
            {
                throw new HubException("Messages must be 1000 characters or less.");
            }

            var currentUser = await GetCurrentQueuedUser();
            var session = _chatMatchService.GetSessionByUserId(currentUser.UserId);
            if (session == null || session.SessionId != sessionId)
            {
                throw new HubException("You are not connected to this chat session.");
            }

            var message = new ChatMessage
            {
                ChatSessionId = sessionId,
                SenderUserId = currentUser.UserId,
                Content = trimmedContent,
                SentAtUtc = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            var payload = new ChatMessageModel
            {
                SessionId = sessionId,
                SenderUserId = currentUser.UserId,
                SenderName = currentUser.FirstNameOrFallback(),
                Content = trimmedContent,
                SentAtUtc = message.SentAtUtc
            };

            await Clients.Client(session.Mentor.ConnectionId).SendAsync("ReceiveMessage", payload);
            await Clients.Client(session.Mentee.ConnectionId).SendAsync("ReceiveMessage", payload);
        }

        public async Task EndChat()
        {
            var currentUser = await GetCurrentQueuedUser();
            var session = _chatMatchService.EndSessionByUserId(currentUser.UserId);
            if (session == null)
            {
                return;
            }

            await MarkSessionEnded(session.SessionId);
            await NotifySessionEnded(session, $"{currentUser.FirstNameOrFallback()} ended the chat.");
        }

        public async Task SkipChat()
        {
            var currentUser = await GetCurrentQueuedUser();
            var session = _chatMatchService.EndSessionByUserId(currentUser.UserId);
            if (session != null)
            {
                await MarkSessionEnded(session.SessionId);
                await NotifySessionEnded(session, $"{currentUser.FirstNameOrFallback()} skipped to the next chat.");
            }

            await JoinQueue();
        }

        private async Task<QueuedUser> GetCurrentQueuedUser()
        {
            var userName = Context.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new HubException("User identity is missing.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
            {
                throw new HubException("User was not found.");
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
            {
                throw new HubException("Complete your profile before starting chat.");
            }

            return new QueuedUser(
                user.Id,
                user.UserName,
                profile.Role ?? user.Role ?? string.Empty,
                string.IsNullOrWhiteSpace(profile.AvatarId) ? "sprout" : profile.AvatarId,
                profile.FirstName ?? string.Empty,
                profile.LastName ?? string.Empty,
                profile.Headline ?? string.Empty,
                profile.Expertise ?? string.Empty,
                profile.Industry ?? string.Empty,
                profile.Topics ?? string.Empty,
                Context.ConnectionId);
        }

        private int GetCurrentUserId()
        {
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? Context.User?.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
            if (user == null)
            {
                throw new HubException("User was not found.");
            }

            return user.Id;
        }

        private async Task NotifySessionEnded(ActiveSessionState session, string reason)
        {
            await Clients.Client(session.Mentor.ConnectionId).SendAsync("ChatEnded", new ChatQueueStateModel
            {
                State = "ended",
                Message = reason
            });

            await Clients.Client(session.Mentee.ConnectionId).SendAsync("ChatEnded", new ChatQueueStateModel
            {
                State = "ended",
                Message = reason
            });
        }

        private async Task MarkSessionEnded(int sessionId)
        {
            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
            {
                return;
            }

            session.Status = "ended";
            session.EndedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _chatHistoryProcessor.CleanupExpiredEndedChatsAsync();
        }

        private static ChatParticipantModel ToParticipantModel(QueuedUser user)
        {
            return new ChatParticipantModel
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Role = user.Role,
                AvatarId = user.AvatarId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Headline = user.Headline,
                Expertise = user.Expertise,
                Industry = user.Industry,
                Topics = user.Topics
            };
        }

        private static string? NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            var normalizedRole = role.Trim().ToLowerInvariant();
            return normalizedRole is "mentor" or "mentee" ? normalizedRole : null;
        }

        private static string GetTargetRole(string role) => role == "mentor" ? "mentee" : "mentor";
    }

    internal static class QueuedUserExtensions
    {
        public static string FirstNameOrFallback(this QueuedUser user)
        {
            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                return user.FirstName;
            }

            return user.UserName;
        }
    }
}
