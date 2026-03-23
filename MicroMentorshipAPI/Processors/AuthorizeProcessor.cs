using MicroMentorshipAPI.Data;
using MicroMentorshipAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MicroMentorshipAPI.Processors
{
    public enum RegisterResult
    {
        Success,
        UserAlreadyExists,
        Failed
    }

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

        internal async Task<RegisterResult> Register(User user)
        {
            var existingUser = await _context.Users.AnyAsync(u => u.UserName == user.UserName);

            if (existingUser)
            {
                return RegisterResult.UserAlreadyExists;
            }

            user.Password = _passwordHasher.HashPassword(user, user.Password);
            _context.Users.Add(user);
            var result = await _context.SaveChangesAsync();
            if (result <= 0)
                return RegisterResult.Failed;

            await _profileProcessor.CreateInitialProfile(user.Id, user.Role);
            return RegisterResult.Success;
        }

        internal async Task<User?> GetUserById(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        internal async Task<User?> LoginWithLinkedIn(
            LinkedInUserInfoResponse linkedInUser,
            string? requestedRole)
        {
            var userName = GetLinkedInUserName(linkedInUser);

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);

            if (dbUser == null)
            {
                dbUser = new User
                {
                    UserName = userName,
                    Password = _passwordHasher.HashPassword(
                        new User { UserName = userName },
                        $"{Guid.NewGuid():N}{Guid.NewGuid():N}!Aa1"),
                    Role = null
                };

                _context.Users.Add(dbUser);
                var result = await _context.SaveChangesAsync();

                if (result <= 0)
                {
                    return null;
                }

                await _profileProcessor.CreateInitialProfile(dbUser.Id, dbUser.Role);
            }

            await PopulateProfileFromLinkedIn(dbUser, linkedInUser);
            return dbUser;
        }

        private async Task PopulateProfileFromLinkedIn(User user, LinkedInUserInfoResponse linkedInUser)
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.FirstName) && !string.IsNullOrWhiteSpace(linkedInUser.GivenName))
            {
                profile.FirstName = linkedInUser.GivenName;
            }

            if (string.IsNullOrWhiteSpace(profile.LastName) && !string.IsNullOrWhiteSpace(linkedInUser.FamilyName))
            {
                profile.LastName = linkedInUser.FamilyName;
            }

            if (!string.IsNullOrWhiteSpace(linkedInUser.Picture))
            {
                profile.ProfilePhotoUrl = linkedInUser.Picture;
            }

            if (string.IsNullOrWhiteSpace(profile.AvatarMode))
            {
                profile.AvatarMode = "illustration";
            }

            await _context.SaveChangesAsync();
        }

        private static string GetLinkedInUserName(LinkedInUserInfoResponse linkedInUser)
        {
            if (!string.IsNullOrWhiteSpace(linkedInUser.Email))
            {
                return linkedInUser.Email.Trim().ToLowerInvariant();
            }

            return $"linkedin-{linkedInUser.Subject}@users.micromentor.invalid";
        }
    }
}
