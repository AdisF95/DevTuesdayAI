using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Tests.Integration.Infrastructure;

/// <summary>
/// Spins up a real PostgreSQL container and applies EF migrations once per test class.
/// Dispose is called automatically by xUnit after the test class finishes.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

    /// <summary>Creates a <see cref="WorldCuppyDbContext" /> connected to the running container.</summary>
    public WorldCuppyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorldCuppyDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new WorldCuppyDbContext(options);
    }

    /// <summary>Starts the container and migrates the schema.</summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    /// <summary>Stops and removes the container.</summary>
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
