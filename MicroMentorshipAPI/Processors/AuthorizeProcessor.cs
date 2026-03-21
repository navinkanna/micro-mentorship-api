using MicroMentorshipAPI.Data;
using MicroMentorshipAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MicroMentorshipAPI.Processors
{
    public class AuthorizeProcessor
    {
        private readonly AppDBContext _context;
        private readonly ProfileProcessor _profileProcessor;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthorizeProcessor(
            AppDBContext context,
            ProfileProcessor profileProcessor,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _profileProcessor = profileProcessor;
            _passwordHasher = passwordHasher;
        }

        internal async Task<User?> Login(User user)
        {
            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == user.UserName);

            if (dbUser == null)
            {
                return null;
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(dbUser, dbUser.Password, user.Password);

            if (verificationResult == PasswordVerificationResult.Success)
            {
                return dbUser;
            }

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                dbUser.Password = _passwordHasher.HashPassword(dbUser, user.Password);
                await _context.SaveChangesAsync();
                return dbUser;
            }

            if (dbUser.Password != user.Password)
            {
                return null;
            }

            dbUser.Password = _passwordHasher.HashPassword(dbUser, user.Password);
            await _context.SaveChangesAsync();
            return dbUser;
        }

        internal async Task<bool> Register(User user)
        {
            var existingUser = await _context.Users.AnyAsync(u => u.UserName == user.UserName);

            if (existingUser)
            {
                return false;
            }

            user.Password = _passwordHasher.HashPassword(user, user.Password);
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
