using Bogus;
using WorldCuppy.Domain;
using WorldCuppy.Features.Predictions;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Predictions;

/// <summary>Integration tests for <see cref="GetUpcomingMatchesWithPredictionsHandler" /> against a real PostgreSQL database.</summary>
public class GetUpcomingMatchesWithPredictionsQueryTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    /// <summary>Instantiates the handler with a fresh DbContext from the fixture.</summary>
    private GetUpcomingMatchesWithPredictionsHandler Handler() =>
        new(db.CreateDbContext());

    private Team BuildTeam() => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        Name       = _faker.Address.Country(),
        Code       = _faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    };

    private Match BuildMatch(Team home, Team away, DateTimeOffset kickoff, MatchStatus status) => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        HomeTeam   = home,
        HomeTeamId = home.Id,
        AwayTeam   = away,
        AwayTeamId = away.Id,
        KickoffUtc = kickoff,
        GameDay    = DateOnly.FromDateTime(kickoff.UtcDateTime),
        Status     = status,
        Venue      = _faker.Address.City(),
    };

    private static Prediction BuildPrediction(Guid userId, Match match, int home, int away) => new()
    {
        Id                  = Guid.NewGuid(),
        UserId              = userId,
        MatchId             = match.Id,
        Match               = match,
        PredictedHomeScore  = home,
        PredictedAwayScore  = away,
        SubmittedAtUtc      = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task GetUpcomingMatchesWithPredictions_WhenUserHasPrediction_ShouldReturnPopulatedPredictionFields()
    {
        await using var ctx = db.CreateDbContext();
        var userId = Guid.NewGuid();
        var home   = BuildTeam();
        var away   = BuildTeam();
        var match  = BuildMatch(home, away, new DateTimeOffset(2045, 7, 1, 18, 0, 0, TimeSpan.Zero), MatchStatus.Scheduled);
        var pred   = BuildPrediction(userId, match, 2, 1);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.Predictions.Add(pred);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetUpcomingMatchesWithPredictionsQuery(userId), CancellationToken.None);

        var row = result.Single(r => r.MatchId == match.Id);
        Assert.Equal(pred.Id, row.PredictionId);
        Assert.Equal(2, row.PredictedHomeScore);
        Assert.Equal(1, row.PredictedAwayScore);
    }

    [Fact]
    public async Task GetUpcomingMatchesWithPredictions_WhenUserHasNoPrediction_ShouldReturnNullPredictionFields()
    {
        await using var ctx = db.CreateDbContext();
        var userId = Guid.NewGuid();
        var home   = BuildTeam();
        var away   = BuildTeam();
        var match  = BuildMatch(home, away, new DateTimeOffset(2045, 7, 2, 18, 0, 0, TimeSpan.Zero), MatchStatus.Scheduled);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetUpcomingMatchesWithPredictionsQuery(userId), CancellationToken.None);

        var row = result.Single(r => r.MatchId == match.Id);
        Assert.Null(row.PredictionId);
        Assert.Null(row.PredictedHomeScore);
        Assert.Null(row.PredictedAwayScore);
    }

    [Fact]
    public async Task GetUpcomingMatchesWithPredictions_WhenMatchIsLive_ShouldExcludeMatch()
    {
        await using var ctx = db.CreateDbContext();
        var userId = Guid.NewGuid();
        var home   = BuildTeam();
        var away   = BuildTeam();
        var match  = BuildMatch(home, away, new DateTimeOffset(2045, 7, 3, 18, 0, 0, TimeSpan.Zero), MatchStatus.Live);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetUpcomingMatchesWithPredictionsQuery(userId), CancellationToken.None);

        Assert.DoesNotContain(result, r => r.MatchId == match.Id);
    }

    [Fact]
    public async Task GetUpcomingMatchesWithPredictions_WhenMatchIsFinished_ShouldExcludeMatch()
    {
        await using var ctx = db.CreateDbContext();
        var userId = Guid.NewGuid();
        var home   = BuildTeam();
        var away   = BuildTeam();
        var match  = BuildMatch(home, away, new DateTimeOffset(2045, 7, 4, 18, 0, 0, TimeSpan.Zero), MatchStatus.Finished);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetUpcomingMatchesWithPredictionsQuery(userId), CancellationToken.None);

        Assert.DoesNotContain(result, r => r.MatchId == match.Id);
    }

    [Fact]
    public async Task GetUpcomingMatchesWithPredictions_WhenMultipleMatches_ShouldReturnOrderedByKickoffAscending()
    {
        await using var ctx = db.CreateDbContext();
        var userId = Guid.NewGuid();
        var teamA  = BuildTeam();
        var teamB  = BuildTeam();

        // Seed in reverse chronological order to prove sorting is applied.
        var later   = new DateTimeOffset(2045, 8, 2, 20, 0, 0, TimeSpan.Zero);
        var earlier = new DateTimeOffset(2045, 8, 1, 15, 0, 0, TimeSpan.Zero);

        var matchLater   = BuildMatch(teamA, teamB, later,   MatchStatus.Scheduled);
        var matchEarlier = BuildMatch(teamB, teamA, earlier, MatchStatus.Scheduled);

        ctx.Teams.AddRange(teamA, teamB);
        ctx.Matches.AddRange(matchLater, matchEarlier);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetUpcomingMatchesWithPredictionsQuery(userId), CancellationToken.None);

        var inserted = result
            .Where(r => r.MatchId == matchEarlier.Id || r.MatchId == matchLater.Id)
            .ToList();

        Assert.Equal(2, inserted.Count);
        Assert.True(inserted[0].KickoffUtc <= inserted[1].KickoffUtc);
    }
}
