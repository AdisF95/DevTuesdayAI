using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence.Configuration;

/// <summary>EF Core mapping configuration for <see cref="Match" />.</summary>
public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    /// <summary>Configures table shape, constraints, and relationships for Match.</summary>
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Venue).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Round).HasConversion<string>();
        builder.Property(m => m.Status).HasConversion<string>();
        builder.Property(m => m.Group).HasMaxLength(20);
        builder.HasIndex(m => m.ExternalId).IsUnique();
        builder.Property(m => m.MatchDuration).HasMaxLength(30);
        builder.HasOne(m => m.HomeTeam).WithMany().HasForeignKey(m => m.HomeTeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.AwayTeam).WithMany().HasForeignKey(m => m.AwayTeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(m => m.GoalEvents).WithOne(g => g.Match).HasForeignKey(g => g.MatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(m => m.BookingEvents).WithOne(b => b.Match).HasForeignKey(b => b.MatchId).OnDelete(DeleteBehavior.Cascade);
    }
}
