using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence.Configuration;

/// <summary>EF Core mapping configuration for <see cref="GoalEvent" />.</summary>
public class GoalEventConfiguration : IEntityTypeConfiguration<GoalEvent>
{
    /// <summary>Configures table shape and constraints for GoalEvent.</summary>
    public void Configure(EntityTypeBuilder<GoalEvent> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Type).HasMaxLength(20).IsRequired();
        builder.Property(g => g.TeamName).HasMaxLength(100).IsRequired();
        builder.Property(g => g.ScorerName).HasMaxLength(100);
        builder.Property(g => g.AssistName).HasMaxLength(100);
    }
}
