using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence.Configuration;

public class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
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
