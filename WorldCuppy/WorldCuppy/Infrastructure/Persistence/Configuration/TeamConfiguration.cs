using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence.Configuration;

/// <summary>EF Core mapping configuration for <see cref="Team" />.</summary>
public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    /// <summary>Configures table shape and constraints for Team.</summary>
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Code).HasMaxLength(3).IsRequired();
        builder.Property(t => t.CrestUrl).HasMaxLength(500);
        builder.HasIndex(t => t.ExternalId).IsUnique();
    }
}
