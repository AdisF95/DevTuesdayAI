using Microsoft.EntityFrameworkCore;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence;

/// <summary>Single EF Core DbContext for the WorldCuppy application.</summary>
public class WorldCuppyDbContext(DbContextOptions<WorldCuppyDbContext> options) : DbContext(options)
{
    /// <summary>All match fixtures.</summary>
    public DbSet<Match> Matches => Set<Match>();

    /// <summary>All national teams.</summary>
    public DbSet<Team> Teams => Set<Team>();

    /// <summary>All user predictions.</summary>
    public DbSet<Prediction> Predictions => Set<Prediction>();

    /// <summary>All registered users.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Applies all <see cref="IEntityTypeConfiguration{T}" /> implementations found in this assembly.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorldCuppyDbContext).Assembly);
    }
}
