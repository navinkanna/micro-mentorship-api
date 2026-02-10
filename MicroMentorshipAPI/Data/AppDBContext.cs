using MicroMentorshipAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace MicroMentorshipAPI.Data
{
    public class AppDBContext: DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

    }
}
