using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence.Configuration;

/// <summary>EF Core mapping configuration for <see cref="GroupStanding" />.</summary>
public class GroupStandingConfiguration : IEntityTypeConfiguration<GroupStanding>
{
    /// <summary>Configures table shape, constraints, and relationships for GroupStanding.</summary>
    public void Configure(EntityTypeBuilder<GroupStanding> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Group).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Form).HasMaxLength(20);
        // One row per team per group — prevents duplicates during upsert.
        builder.HasIndex(s => new { s.Group, s.TeamId }).IsUnique();
        builder.HasOne(s => s.Team).WithMany().HasForeignKey(s => s.TeamId).OnDelete(DeleteBehavior.Restrict);
    }
}
