using Microsoft.EntityFrameworkCore;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence;

public class WorldCuppyDbContext(DbContextOptions<WorldCuppyDbContext> options) : DbContext(options)
{
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorldCuppyDbContext).Assembly);
    }
}
