using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Domain;
using WorldCuppy.Infrastructure.FootballData;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Sync;

/// <summary>Syncs teams and matches from football-data.org into the local database.</summary>
public record SyncCommand : IRequest<SyncResult>;

/// <summary>Summary returned after a sync operation.</summary>
public record SyncResult(int TeamsUpserted, int MatchesUpserted);

/// <summary>Handles <see cref="SyncCommand" />.</summary>
public class SyncHandler(WorldCuppyDbContext db, FootballDataClient client)
    : IRequestHandler<SyncCommand, SyncResult>
{
    /// <summary>Fetches teams then matches from the API, upserts both into the database.</summary>
    public async Task<SyncResult> Handle(SyncCommand request, CancellationToken cancellationToken)
    {
        var teamsUpserted = await SyncTeamsAsync(cancellationToken);
        var matchesUpserted = await SyncMatchesAsync(cancellationToken);
        return new SyncResult(teamsUpserted, matchesUpserted);
    }

    private async Task<int> SyncTeamsAsync(CancellationToken ct)
    {
        var fdTeams = await client.GetTeamsAsync(ct);

        var existing = await db.Teams
            .Where(t => t.ExternalId != 0)
            .ToDictionaryAsync(t => t.ExternalId, ct);

        foreach (var fd in fdTeams)
        {
            if (existing.TryGetValue(fd.Id, out var team))
            {
                team.Name = fd.Name;
                team.Code = fd.Tla;
                team.CrestUrl = fd.Crest;
            }
            else
            {
                db.Teams.Add(new Team
                {
                    Id = Guid.NewGuid(),
                    ExternalId = fd.Id,
                    Name = fd.Name,
                    Code = fd.Tla,
                    CrestUrl = fd.Crest,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return fdTeams.Count;
    }

    private async Task<int> SyncMatchesAsync(CancellationToken ct)
    {
        var fdMatches = await client.GetMatchesAsync(ct);

        var teamsByExternalId = await db.Teams
            .Where(t => t.ExternalId != 0)
            .ToDictionaryAsync(t => t.ExternalId, ct);

        var existingMatches = await db.Matches
            .Where(m => m.ExternalId != 0)
            .ToDictionaryAsync(m => m.ExternalId, ct);

        var upserted = 0;

        foreach (var fd in fdMatches)
        {
            // Skip knockout matches where teams are not yet determined
            if (fd.HomeTeam.Id is null || fd.AwayTeam.Id is null)
            {
                continue;
            }

            if (!teamsByExternalId.TryGetValue(fd.HomeTeam.Id.Value, out var homeTeam) ||
                !teamsByExternalId.TryGetValue(fd.AwayTeam.Id.Value, out var awayTeam))
            {
                continue;
            }

            var kickoff = DateTimeOffset.Parse(fd.UtcDate, System.Globalization.CultureInfo.InvariantCulture);
            var status = MapStatus(fd.Status);
            var round = MapRound(fd.Stage);

            if (existingMatches.TryGetValue(fd.Id, out var match))
            {
                match.Status = status;
                match.KickoffUtc = kickoff;
                match.HomeScore = fd.Score.FullTime.Home;
                match.AwayScore = fd.Score.FullTime.Away;
                // Venue may be confirmed late; only overwrite if the API provides one
                if (fd.Venue is not null)
                {
                    match.Venue = fd.Venue;
                }
            }
            else
            {
                db.Matches.Add(new Match
                {
                    Id = Guid.NewGuid(),
                    ExternalId = fd.Id,
                    HomeTeamId = homeTeam.Id,
                    HomeTeam = homeTeam,
                    AwayTeamId = awayTeam.Id,
                    AwayTeam = awayTeam,
                    KickoffUtc = kickoff,
                    GameDay = DateOnly.FromDateTime(kickoff.UtcDateTime),
                    Round = round,
                    Status = status,
                    Group = fd.Group,
                    Venue = fd.Venue ?? "TBD",
                    HomeScore = fd.Score.FullTime.Home,
                    AwayScore = fd.Score.FullTime.Away,
                });
            }

            upserted++;
        }

        await db.SaveChangesAsync(ct);
        return upserted;
    }

    private static MatchStatus MapStatus(string status) => status switch
    {
        "IN_PLAY" or "PAUSED" or "SUSPENDED" => MatchStatus.Live,
        "FINISHED" or "AWARDED" => MatchStatus.Finished,
        "POSTPONED" => MatchStatus.Postponed,
        "CANCELLED" => MatchStatus.Cancelled,
        _ => MatchStatus.Scheduled, // SCHEDULED, TIMED
    };

    private static KnockoutRound? MapRound(string stage) => stage switch
    {
        "LAST_32" or "ROUND_OF_32" => KnockoutRound.RoundOf32,
        "LAST_16" or "ROUND_OF_16" => KnockoutRound.RoundOf16,
        "QUARTER_FINALS" => KnockoutRound.QuarterFinal,
        "SEMI_FINALS" => KnockoutRound.SemiFinal,
        "FINAL" => KnockoutRound.Final,
        _ => null, // GROUP_STAGE and anything unrecognised
    };
}
