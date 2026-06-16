using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence.Configuration;

/// <summary>EF Core mapping configuration for <see cref="Prediction" />.</summary>
public class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
    /// <summary>Configures table shape, constraints, and relationships for Prediction.</summary>
    public void Configure(EntityTypeBuilder<Prediction> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.PredictedHomeScore).IsRequired();
        builder.Property(p => p.PredictedAwayScore).IsRequired();
        builder.Property(p => p.SubmittedAtUtc).IsRequired();
        builder.HasOne(p => p.Match)
            .WithMany()
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => new { p.UserId, p.MatchId }).IsUnique();
    }
}
