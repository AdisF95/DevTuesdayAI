using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence.Configuration;

/// <summary>EF Core mapping configuration for <see cref="BookingEvent" />.</summary>
public class BookingEventConfiguration : IEntityTypeConfiguration<BookingEvent>
{
    /// <summary>Configures table shape and constraints for BookingEvent.</summary>
    public void Configure(EntityTypeBuilder<BookingEvent> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.CardType).HasMaxLength(20).IsRequired();
        builder.Property(b => b.PlayerName).HasMaxLength(100);
    }
}
