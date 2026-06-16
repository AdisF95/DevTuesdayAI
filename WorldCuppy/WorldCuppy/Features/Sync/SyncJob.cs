using MediatR;

namespace WorldCuppy.Features.Sync;

/// <summary>Hangfire background job that syncs match data from football-data.org on a schedule.</summary>
public partial class SyncJob(ISender sender, ILogger<SyncJob> logger)
{
    /// <summary>Pulls the latest teams and match results from football-data.org into the database.</summary>
    public async Task ExecuteAsync()
    {
        LogSyncStarted(logger);
        var result = await sender.Send(new SyncCommand());
        LogSyncComplete(logger, result.TeamsUpserted, result.MatchesUpserted);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Football-data sync started")]
    private static partial void LogSyncStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Football-data sync complete — {TeamsUpserted} teams, {MatchesUpserted} matches upserted")]
    private static partial void LogSyncComplete(ILogger logger, int teamsUpserted, int matchesUpserted);
}
