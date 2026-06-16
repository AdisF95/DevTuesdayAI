using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Domain;
using WorldCuppy.Infrastructure.FootballData;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Sync;

/// <summary>Syncs teams, matches, standings, and match events from football-data.org into the local database.</summary>
public record SyncCommand : IRequest<SyncResult>;

/// <summary>Summary returned after a sync operation.</summary>
public record SyncResult(int TeamsUpserted, int MatchesUpserted, int StandingsUpserted, int GoalEventsAdded, int BookingEventsAdded);

/// <summary>Handles <see cref="SyncCommand" />.</summary>
public class SyncHandler(WorldCuppyDbContext db, FootballDataClient client)
    : IRequestHandler<SyncCommand, SyncResult>
{
    /// <summary>Fetches teams, matches, standings, and match events from the API and upserts them into the database.</summary>
    public async Task<SyncResult> Handle(SyncCommand request, CancellationToken cancellationToken)
    {
        var teamsUpserted     = await SyncTeamsAsync(cancellationToken);
        var matchesUpserted   = await SyncMatchesAsync(cancellationToken);
        var standingsUpserted = await SyncStandingsAsync(cancellationToken);
        var (goalsAdded, bookingsAdded) = await SyncMatchEventsAsync(cancellationToken);
        return new SyncResult(teamsUpserted, matchesUpserted, standingsUpserted, goalsAdded, bookingsAdded);
    }

    private async Task<int> SyncTeamsAsync(CancellationToken ct)
    {
        // Teams are fixed once the tournament starts — skip the API call if already populated.
        if (await db.Teams.AnyAsync(t => t.ExternalId != 0, ct))
        {
            return 0;
        }

        var fdTeams = await client.GetTeamsAsync(ct);

        var existing = await db.Teams
            .Where(t => t.ExternalId != 0)
            .ToDictionaryAsync(t => t.ExternalId, ct);

        foreach (var fd in fdTeams)
        {
            if (existing.TryGetValue(fd.Id, out var team))
            {
                team.Name     = fd.Name;
                team.Code     = fd.Tla;
                team.CrestUrl = fd.Crest;
            }
            else
            {
                db.Teams.Add(new Team
                {
                    Id         = Guid.NewGuid(),
                    ExternalId = fd.Id,
                    Name       = fd.Name,
                    Code       = fd.Tla,
                    CrestUrl   = fd.Crest,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return fdTeams.Count;
    }

    private async Task<int> SyncMatchesAsync(CancellationToken ct)
    {
        // On first run the DB is empty — fetch the full schedule.
        // On every subsequent run scope to yesterday → +5 days so we skip the
        // ever-growing tail of already-finished matches and keep each sync cheap.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hasMatches = await db.Matches.AnyAsync(ct);
        var fdMatches = hasMatches
            ? await client.GetMatchesAsync(dateFrom: today.AddDays(-1), dateTo: today.AddDays(5), ct)
            : await client.GetMatchesAsync(cancellationToken: ct);

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
            var status  = MapStatus(fd.Status);
            var round   = MapRound(fd.Stage);

            if (existingMatches.TryGetValue(fd.Id, out var match))
            {
                // Finished matches have authoritative scores — no need to re-apply API data.
                if (match.Status == MatchStatus.Finished)
                {
                    continue;
                }

                match.Status             = status;
                match.KickoffUtc         = kickoff;
                match.HomeScore          = fd.Score.FullTime.Home;
                match.AwayScore          = fd.Score.FullTime.Away;
                match.HalfTimeHomeScore  = fd.Score.HalfTime?.Home;
                match.HalfTimeAwayScore  = fd.Score.HalfTime?.Away;
                match.ExtraTimeHomeScore = fd.Score.ExtraTime?.Home;
                match.ExtraTimeAwayScore = fd.Score.ExtraTime?.Away;
                match.PenaltyHomeScore   = fd.Score.Penalties?.Home;
                match.PenaltyAwayScore   = fd.Score.Penalties?.Away;
                match.MatchDuration      = fd.Score.Duration;
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
                    Id                  = Guid.NewGuid(),
                    ExternalId          = fd.Id,
                    HomeTeamId          = homeTeam.Id,
                    HomeTeam            = homeTeam,
                    AwayTeamId          = awayTeam.Id,
                    AwayTeam            = awayTeam,
                    KickoffUtc          = kickoff,
                    GameDay             = DateOnly.FromDateTime(kickoff.UtcDateTime),
                    Round               = round,
                    Status              = status,
                    Group               = fd.Group,
                    Venue               = fd.Venue ?? "TBD",
                    HomeScore           = fd.Score.FullTime.Home,
                    AwayScore           = fd.Score.FullTime.Away,
                    HalfTimeHomeScore   = fd.Score.HalfTime?.Home,
                    HalfTimeAwayScore   = fd.Score.HalfTime?.Away,
                    ExtraTimeHomeScore  = fd.Score.ExtraTime?.Home,
                    ExtraTimeAwayScore  = fd.Score.ExtraTime?.Away,
                    PenaltyHomeScore    = fd.Score.Penalties?.Home,
                    PenaltyAwayScore    = fd.Score.Penalties?.Away,
                    MatchDuration       = fd.Score.Duration,
                });
            }

            upserted++;
        }

        await db.SaveChangesAsync(ct);
        return upserted;
    }

    private async Task<int> SyncStandingsAsync(CancellationToken ct)
    {
        var fdStandings = await client.GetStandingsAsync(ct);

        var teamsByExternalId = await db.Teams
            .Where(t => t.ExternalId != 0)
            .ToDictionaryAsync(t => t.ExternalId, ct);

        var existing = await db.GroupStandings
            .ToDictionaryAsync(s => (s.Group, s.TeamId), ct);

        var upserted = 0;

        foreach (var fdGroup in fdStandings.Where(g => g.Group is not null))
        {
            foreach (var entry in fdGroup.Table)
            {
                if (entry.Team.Id is null ||
                    !teamsByExternalId.TryGetValue(entry.Team.Id.Value, out var team))
                {
                    continue;
                }

                var key = (fdGroup.Group!, team.Id);

                if (existing.TryGetValue(key, out var standing))
                {
                    standing.Position       = entry.Position;
                    standing.PlayedGames    = entry.PlayedGames;
                    standing.Won            = entry.Won;
                    standing.Draw           = entry.Draw;
                    standing.Lost           = entry.Lost;
                    standing.GoalsFor       = entry.GoalsFor;
                    standing.GoalsAgainst   = entry.GoalsAgainst;
                    standing.GoalDifference = entry.GoalDifference;
                    standing.Points         = entry.Points;
                    standing.Form           = entry.Form;
                }
                else
                {
                    db.GroupStandings.Add(new GroupStanding
                    {
                        Id              = Guid.NewGuid(),
                        Group           = fdGroup.Group!,
                        TeamId          = team.Id,
                        Team            = team,
                        Position        = entry.Position,
                        PlayedGames     = entry.PlayedGames,
                        Won             = entry.Won,
                        Draw            = entry.Draw,
                        Lost            = entry.Lost,
                        GoalsFor        = entry.GoalsFor,
                        GoalsAgainst    = entry.GoalsAgainst,
                        GoalDifference  = entry.GoalDifference,
                        Points          = entry.Points,
                        Form            = entry.Form,
                    });
                }

                upserted++;
            }
        }

        await db.SaveChangesAsync(ct);
        return upserted;
    }

    private async Task<(int goalsAdded, int bookingsAdded)> SyncMatchEventsAsync(CancellationToken ct)
    {
        // Fetch events for finished matches that have not been synced yet.
        // EventsSynced is set after a successful detail call regardless of goal/booking count,
        // so 0-0 matches are not re-fetched every cycle.
        // Capped at 10 per cycle so the sync always completes well within 15 minutes;
        // any remaining matches carry forward and are picked up on the next cycle.
        var pending = await db.Matches
            .Where(m => m.Status == MatchStatus.Finished && !m.EventsSynced)
            .Select(m => new
            {
                m.Id,
                m.ExternalId,
                HomeTeamExternalId = m.HomeTeam.ExternalId,
            })
            .Take(10)
            .ToListAsync(ct);

        var totalGoals    = 0;
        var totalBookings = 0;

        foreach (var matchRef in pending)
        {
            try
            {
                var detail = await client.GetMatchDetailAsync(matchRef.ExternalId, ct);
                if (detail is null)
                {
                    continue;
                }

                foreach (var goal in detail.Goals ?? [])
                {
                    if (goal.Minute is null || goal.Team?.Id is null)
                    {
                        continue;
                    }

                    db.GoalEvents.Add(new GoalEvent
                    {
                        Id         = Guid.NewGuid(),
                        MatchId    = matchRef.Id,
                        Minute     = goal.Minute.Value,
                        ScorerName = goal.Scorer?.Name,
                        AssistName = goal.Assist?.Name,
                        Type       = goal.Type ?? "REGULAR",
                        TeamName   = goal.Team.Name ?? string.Empty,
                        IsHomeTeam = goal.Team.Id == matchRef.HomeTeamExternalId,
                    });
                    totalGoals++;
                }

                foreach (var booking in detail.Bookings ?? [])
                {
                    if (booking.Minute is null || booking.Card is null)
                    {
                        continue;
                    }

                    db.BookingEvents.Add(new BookingEvent
                    {
                        Id         = Guid.NewGuid(),
                        MatchId    = matchRef.Id,
                        Minute     = booking.Minute.Value,
                        PlayerName = booking.Player?.Name,
                        CardType   = booking.Card,
                        IsHomeTeam = booking.Team?.Id == matchRef.HomeTeamExternalId,
                    });
                    totalBookings++;
                }

                await db.SaveChangesAsync(ct);

                // Mark the match as synced regardless of goal/booking count so it is
                // never re-fetched (a 0-0 match with no cards would otherwise loop forever).
                await db.Matches
                    .Where(m => m.Id == matchRef.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.EventsSynced, true), ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // Rate limit hit — stop event processing for this cycle.
                // Remaining matches carry forward; no blocking retry here.
                break;
            }
            catch (Exception)
            {
                // Any other error on a single match should not abort the whole sync.
                // The match will be retried on the next sync cycle.
            }

            // Stay within the football-data.org free-tier rate limit (~10 req/min).
            // Capped batch (≤10) + 10 s spacing keeps event calls to ≤6/min,
            // leaving headroom for the 2 fixed calls (matches + standings).
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }

        return (totalGoals, totalBookings);
    }

    internal static MatchStatus MapStatus(string status) => status switch
    {
        "IN_PLAY" or "PAUSED" or "SUSPENDED" => MatchStatus.Live,
        "FINISHED" or "AWARDED"              => MatchStatus.Finished,
        "POSTPONED"                          => MatchStatus.Postponed,
        "CANCELLED"                          => MatchStatus.Cancelled,
        _                                    => MatchStatus.Scheduled, // SCHEDULED, TIMED
    };

    internal static KnockoutRound? MapRound(string stage) => stage switch
    {
        "LAST_32" or "ROUND_OF_32" => KnockoutRound.RoundOf32,
        "LAST_16" or "ROUND_OF_16" => KnockoutRound.RoundOf16,
        "QUARTER_FINALS"           => KnockoutRound.QuarterFinal,
        "SEMI_FINALS"              => KnockoutRound.SemiFinal,
        "FINAL"                    => KnockoutRound.Final,
        _                          => null, // GROUP_STAGE and anything unrecognised
    };
}
