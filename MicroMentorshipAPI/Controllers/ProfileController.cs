using MicroMentorshipAPI.Models;
using MicroMentorshipAPI.Processors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroMentorshipAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ProfileProcessor _profileProcessor;

        public ProfileController(ProfileProcessor profileProcessor)
        {
            _profileProcessor = profileProcessor;
        }

        [HttpGet]
        public async Task<ActionResult<ProfileModel>> Get()
        {
            var profile = await _profileProcessor.GetByUserName(User.Identity?.Name ?? string.Empty);
            if (profile == null)
                return NotFound();

            return Ok(ToModel(profile));
        }

        [HttpPost]
        public async Task<ActionResult<ProfileModel>> Create(ProfileModel profileModel)
        {
            var profile = await _profileProcessor.Create(User.Identity?.Name ?? string.Empty, profileModel);
            if (profile == null)
                return Conflict("Profile already exists or user was not found.");

            return Ok(ToModel(profile));
        }

        [HttpPut]
        public async Task<ActionResult<ProfileModel>> Update(ProfileModel profileModel)
        {
            var profile = await _profileProcessor.Update(User.Identity?.Name ?? string.Empty, profileModel);
            if (profile == null)
                return NotFound();

            return Ok(ToModel(profile));
        }

        private static ProfileModel ToModel(Profile profile)
        {
            return new ProfileModel
            {
                AvatarId = profile.AvatarId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Role = profile.Role,
                Expertise = profile.Expertise,
                YearsOfExperience = profile.YearsOfExperience,
                Industry = profile.Industry,
                Company = profile.Company,
                Location = profile.Location,
                Headline = profile.Headline,
                Bio = profile.Bio,
                Topics = profile.Topics,
                HelpfulFeedbackCount = profile.HelpfulFeedbackCount
            };
        }
    }
}
