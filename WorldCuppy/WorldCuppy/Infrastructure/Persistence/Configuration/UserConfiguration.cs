using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Persistence.Configuration;

/// <summary>EF Core mapping configuration for <see cref="User" />.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>Configures table shape, constraints, and unique indexes for User.</summary>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.CreatedAtUtc).IsRequired();

        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
