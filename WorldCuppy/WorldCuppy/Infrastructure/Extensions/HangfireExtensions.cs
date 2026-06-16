using Hangfire;
using Hangfire.PostgreSql;
using WorldCuppy.Features.Sync;

namespace WorldCuppy.Infrastructure.Extensions;

/// <summary>Service and middleware registration for Hangfire background jobs.</summary>
public static class HangfireExtensions
{
    /// <summary>Registers Hangfire with PostgreSQL storage using the existing connection string.</summary>
    public static IServiceCollection AddHangfireWithPostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();

        return services;
    }

    /// <summary>
    /// Maps the Hangfire dashboard and registers all recurring jobs.
    /// Dashboard is only exposed in Development to avoid leaking job details in production.
    /// Authorization is cleared in dev so Docker bridge traffic is not blocked.
    /// </summary>
    public static WebApplication UseHangfire(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [],
            });
        }

        // Sync every 15 minutes — fine-grained enough to catch results within a quarter-hour of full time.
        RecurringJob.AddOrUpdate<SyncJob>(
            "sync-football-data",
            job => job.ExecuteAsync(),
            "*/15 * * * *");

        // Enqueue an immediate sync on every startup so the DB is populated straight away.
        BackgroundJob.Enqueue<SyncJob>(job => job.ExecuteAsync());

        return app;
    }
}
