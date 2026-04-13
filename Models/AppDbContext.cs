using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ayul_dayusy.Models
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Petition> Petitions { get; set; }
        public DbSet<PetitionVote> PetitionVotes { get; set; }
    }
}
