
using MicroMentorshipAPI.Data;
using MicroMentorshipAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroMentorshipAPI.Processors
{
    public class AuthorizeProcessor
    {
        private AppDBContext _context;
        public AuthorizeProcessor(AppDBContext context)
        {
            _context = context;
        }

        internal async Task<User> Login(User user)
        {
            var authorizedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == user.UserName && u.Password == user.Password);
            return authorizedUser;
        }

        internal async Task<bool> Register(User user)
        {
            _context.Users.Add(user);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}
