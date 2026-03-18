using MicroMentorshipAPI.Data;
using MicroMentorshipAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroMentorshipAPI.Processors
{
    public class AuthorizeProcessor
    {
        private readonly AppDBContext _context;
        private readonly ProfileProcessor _profileProcessor;

        public AuthorizeProcessor(AppDBContext context, ProfileProcessor profileProcessor)
        {
            _context = context;
            _profileProcessor = profileProcessor;
        }

        internal async Task<User?> Login(User user)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserName == user.UserName && u.Password == user.Password);
        }

        internal async Task<bool> Register(User user)
        {
            _context.Users.Add(user);
            var result = await _context.SaveChangesAsync();
            if (result <= 0)
                return false;

            await _profileProcessor.CreateInitialProfile(user.Id, user.Role);
            return true;
        }

        internal async Task<User?> GetUserById(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
