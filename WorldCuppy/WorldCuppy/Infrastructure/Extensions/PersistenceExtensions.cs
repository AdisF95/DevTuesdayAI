using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Infrastructure.Extensions;

/// <summary>Extension methods for registering EF Core and the database connection.</summary>
public static class PersistenceExtensions
{
    /// <summary>Registers <see cref="WorldCuppyDbContext" /> with the Npgsql provider using the "Default" connection string.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WorldCuppyDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        return services;
    }
}
