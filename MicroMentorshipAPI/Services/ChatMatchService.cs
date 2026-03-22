using System.Collections.Concurrent;
using MicroMentorshipAPI.Models;

namespace MicroMentorshipAPI.Services
{
    public class ChatMatchService
    {
        private readonly object _sync = new();
        private readonly LinkedList<QueuedUser> _mentors = new();
        private readonly LinkedList<QueuedUser> _mentees = new();
        private readonly ConcurrentDictionary<int, QueuedUser> _queuedUsers = new();
        private readonly ConcurrentDictionary<string, int> _connectionToUserId = new();
        private readonly ConcurrentDictionary<int, ActiveSessionState> _activeSessions = new();
        private readonly ConcurrentDictionary<int, int> _userToSessionId = new();

        public void RegisterConnection(int userId, string connectionId)
        {
            _connectionToUserId[connectionId] = userId;
        }

        public void RemoveConnection(string connectionId)
        {
            _connectionToUserId.TryRemove(connectionId, out _);
        }

        public QueuedUser? EnqueueOrMatch(QueuedUser user)
        {
            lock (_sync)
            {
                RemoveFromQueueUnsafe(user.UserId);

                var oppositeQueue = user.Role == "mentor" ? _mentees : _mentors;
                var matchedNode = FindBestMatch(oppositeQueue, user);
                if (matchedNode != null)
                {
                    oppositeQueue.Remove(matchedNode);
                    _queuedUsers.TryRemove(matchedNode.Value.UserId, out _);
                    return matchedNode.Value;
                }

                var targetQueue = user.Role == "mentor" ? _mentors : _mentees;
                targetQueue.AddLast(user);
                _queuedUsers[user.UserId] = user;
                return null;
            }
        }

        public void CancelQueue(int userId)
        {
            lock (_sync)
            {
                RemoveFromQueueUnsafe(userId);
            }
        }

        public ActiveSessionState RegisterSession(int sessionId, QueuedUser mentor, QueuedUser mentee)
        {
            var session = new ActiveSessionState(sessionId, mentor, mentee);
            _activeSessions[sessionId] = session;
            _userToSessionId[mentor.UserId] = sessionId;
            _userToSessionId[mentee.UserId] = sessionId;
            return session;
        }

        public ActiveSessionState? GetSessionByUserId(int userId)
        {
            if (_userToSessionId.TryGetValue(userId, out var sessionId) &&
                _activeSessions.TryGetValue(sessionId, out var session))
            {
                return session;
            }

            return null;
        }

        public ActiveSessionState? EndSessionByUserId(int userId)
        {
            if (!_userToSessionId.TryGetValue(userId, out var sessionId))
            {
                return null;
            }

            return EndSessionBySessionId(sessionId);
        }

        public ActiveSessionState? EndSessionBySessionId(int sessionId)
        {
            if (!_activeSessions.TryRemove(sessionId, out var session))
            {
                return null;
            }

            _userToSessionId.TryRemove(session.Mentor.UserId, out _);
            _userToSessionId.TryRemove(session.Mentee.UserId, out _);
            return session;
        }

        private void RemoveFromQueueUnsafe(int userId)
        {
            if (!_queuedUsers.TryRemove(userId, out var queuedUser))
            {
                return;
            }

            var queue = queuedUser.Role == "mentor" ? _mentors : _mentees;
            var node = queue.First;
            while (node != null)
            {
                if (node.Value.UserId == userId)
                {
                    queue.Remove(node);
                    return;
                }

                node = node.Next;
            }
        }

        private static LinkedListNode<QueuedUser>? FindBestMatch(LinkedList<QueuedUser> queue, QueuedUser candidate)
        {
            LinkedListNode<QueuedUser>? current = queue.First;
            LinkedListNode<QueuedUser>? best = null;
            var bestScore = int.MinValue;

            while (current != null)
            {
                var score = Score(current.Value, candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = current;
                }

                current = current.Next;
            }

            return best;
        }

        private static int Score(QueuedUser existing, QueuedUser incoming)
        {
            var score = 0;

            if (SharesToken(existing.Topics, incoming.Topics))
            {
                score += 5;
            }

            if (SharesToken(existing.Expertise, incoming.Expertise))
            {
                score += 3;
            }

            if (!string.IsNullOrWhiteSpace(existing.Industry) &&
                existing.Industry.Equals(incoming.Industry, StringComparison.OrdinalIgnoreCase))
            {
                score += 2;
            }

            return score;
        }

        private static bool SharesToken(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            var leftTokens = left.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var rightTokens = right.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return leftTokens.Intersect(rightTokens, StringComparer.OrdinalIgnoreCase).Any();
        }
    }

    public record QueuedUser(
        int UserId,
        string UserName,
        string Role,
        string AvatarId,
        string FirstName,
        string LastName,
        string Headline,
        string Expertise,
        string Industry,
        string Topics,
        int HelpfulFeedbackCount,
        string ConnectionId);

    public record ActiveSessionState(int SessionId, QueuedUser Mentor, QueuedUser Mentee)
    {
        public QueuedUser GetPartner(int userId) => Mentor.UserId == userId ? Mentee : Mentor;
    }
}
