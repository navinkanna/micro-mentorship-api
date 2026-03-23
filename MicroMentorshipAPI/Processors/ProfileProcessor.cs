using MicroMentorshipAPI.Data;
using MicroMentorshipAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroMentorshipAPI.Processors
{
    public class ProfileProcessor
    {
        private readonly AppDBContext _context;

        public ProfileProcessor(AppDBContext context)
        {
            _context = context;
        }

        public async Task<Profile?> GetByUserName(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
                return null;

            return await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        }

        public async Task<Profile?> Create(string userName, ProfileModel profileModel)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
                return null;

            var existingProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (existingProfile != null)
                return null;

            var profile = new Profile { UserId = user.Id };
            ApplyModel(profile, profileModel);
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            return profile;
        }

        public async Task<Profile?> Update(string userName, ProfileModel profileModel)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null)
                return null;

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
                return null;

            ApplyModel(profile, profileModel);
            await _context.SaveChangesAsync();

            return profile;
        }

        public async Task CreateInitialProfile(int userId, string? role)
        {
            var existingProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (existingProfile != null)
                return;

            var profile = new Profile
            {
                UserId = userId,
                Role = role,
                AvatarId = "sprout",
                AvatarMode = "illustration"
            };

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        private static void ApplyModel(Profile profile, ProfileModel profileModel)
        {
            profile.AvatarId = string.IsNullOrWhiteSpace(profileModel.AvatarId) ? "sprout" : profileModel.AvatarId;
            profile.AvatarMode = NormalizeAvatarMode(profileModel.AvatarMode, profileModel.ProfilePhotoUrl);
            profile.ProfilePhotoUrl = string.IsNullOrWhiteSpace(profileModel.ProfilePhotoUrl)
                ? null
                : profileModel.ProfilePhotoUrl.Trim();
            profile.FirstName = profileModel.FirstName;
            profile.LastName = profileModel.LastName;
            profile.Role = profileModel.Role;
            profile.Expertise = profileModel.Expertise;
            profile.YearsOfExperience = profileModel.YearsOfExperience;
            profile.Industry = profileModel.Industry;
            profile.Company = profileModel.Company;
            profile.Location = profileModel.Location;
            profile.Headline = profileModel.Headline;
            profile.Bio = profileModel.Bio;
            profile.Topics = profileModel.Topics;
        }

        private static string NormalizeAvatarMode(string? avatarMode, string? profilePhotoUrl)
        {
            var normalizedMode = avatarMode?.Trim().ToLowerInvariant();

            if (normalizedMode == "photo" && !string.IsNullOrWhiteSpace(profilePhotoUrl))
            {
                return "photo";
            }

            return "illustration";
        }
    }
}
