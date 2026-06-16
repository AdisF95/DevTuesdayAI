using Bogus;
using WorldCuppy.Domain;
using WorldCuppy.Features.Matches;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Matches;

/// <summary>Integration tests for <see cref="GetMatchDetailHandler" /> against a real PostgreSQL database.</summary>
public class GetMatchDetailQueryTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    private GetMatchDetailHandler Handler() => new(db.CreateDbContext());

    private Team BuildTeam() => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        Name       = _faker.Address.Country(),
        Code       = _faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    };

    private Match BuildFinishedMatch(Team home, Team away) => new()
    {
        Id                 = Guid.NewGuid(),
        ExternalId         = Math.Abs(Guid.NewGuid().GetHashCode()),
        HomeTeam           = home,
        HomeTeamId         = home.Id,
        AwayTeam           = away,
        AwayTeamId         = away.Id,
        KickoffUtc         = new DateTimeOffset(2026, 6, 15, 19, 0, 0, TimeSpan.Zero),
        GameDay            = new DateOnly(2026, 6, 15),
        Status             = MatchStatus.Finished,
        Venue              = _faker.Address.City(),
        Group              = "GROUP_B",
        HomeScore          = 2,
        AwayScore          = 1,
        HalfTimeHomeScore  = 1,
        HalfTimeAwayScore  = 0,
        MatchDuration      = "REGULAR",
    };

    [Fact]
    public async Task GetMatchDetail_WhenMatchDoesNotExist_ShouldReturnNull()
    {
        var result = await Handler().Handle(new GetMatchDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMatchDetail_ShouldReturnCoreMatchFields()
    {
        await using var ctx = db.CreateDbContext();
        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildFinishedMatch(home, away);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetMatchDetailQuery(match.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(match.Id, result.Id);
        Assert.Equal(home.Name, result.HomeTeam);
        Assert.Equal(away.Name, result.AwayTeam);
        Assert.Equal(match.HomeScore, result.HomeScore);
        Assert.Equal(match.AwayScore, result.AwayScore);
        Assert.Equal(match.HalfTimeHomeScore, result.HalfTimeHomeScore);
        Assert.Equal(match.HalfTimeAwayScore, result.HalfTimeAwayScore);
        Assert.Equal(match.MatchDuration, result.MatchDuration);
    }

    [Fact]
    public async Task GetMatchDetail_ShouldReturnGoalsOrderedByMinute()
    {
        await using var ctx = db.CreateDbContext();
        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildFinishedMatch(home, away);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.GoalEvents.AddRange(
            new GoalEvent { Id = Guid.NewGuid(), MatchId = match.Id, Minute = 78, ScorerName = "Scorer B", Type = "REGULAR",  TeamName = away.Name, IsHomeTeam = false },
            new GoalEvent { Id = Guid.NewGuid(), MatchId = match.Id, Minute = 23, ScorerName = "Scorer A", Type = "PENALTY",  TeamName = home.Name, IsHomeTeam = true  },
            new GoalEvent { Id = Guid.NewGuid(), MatchId = match.Id, Minute = 55, ScorerName = "Scorer C", Type = "OWN_GOAL", TeamName = away.Name, IsHomeTeam = false });
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetMatchDetailQuery(match.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.Goals.Count);
        Assert.Equal([23, 55, 78], result.Goals.Select(g => g.Minute));
        Assert.Equal("PENALTY",  result.Goals[0].Type);
        Assert.True(result.Goals[0].IsHomeTeam);
        Assert.False(result.Goals[1].IsHomeTeam);
    }

    [Fact]
    public async Task GetMatchDetail_ShouldReturnBookingsOrderedByMinute()
    {
        await using var ctx = db.CreateDbContext();
        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildFinishedMatch(home, away);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.BookingEvents.AddRange(
            new BookingEvent { Id = Guid.NewGuid(), MatchId = match.Id, Minute = 67, PlayerName = "Player B", CardType = "RED_CARD",    IsHomeTeam = false },
            new BookingEvent { Id = Guid.NewGuid(), MatchId = match.Id, Minute = 34, PlayerName = "Player A", CardType = "YELLOW_CARD", IsHomeTeam = true  });
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetMatchDetailQuery(match.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Bookings.Count);
        Assert.Equal([34, 67], result.Bookings.Select(b => b.Minute));
        Assert.Equal("YELLOW_CARD", result.Bookings[0].CardType);
        Assert.Equal("RED_CARD",    result.Bookings[1].CardType);
    }

    [Fact]
    public async Task GetMatchDetail_WhenMatchHasNoEvents_ShouldReturnEmptyCollections()
    {
        await using var ctx = db.CreateDbContext();
        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildFinishedMatch(home, away);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetMatchDetailQuery(match.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Goals);
        Assert.Empty(result.Bookings);
    }

    [Fact]
    public async Task GetMatchDetail_ShouldIncludeExtendedScoresForExtraTimeMatch()
    {
        await using var ctx = db.CreateDbContext();
        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildFinishedMatch(home, away);
        match.MatchDuration      = "PENALTY_SHOOTOUT";
        match.ExtraTimeHomeScore = 2;
        match.ExtraTimeAwayScore = 2;
        match.PenaltyHomeScore   = 4;
        match.PenaltyAwayScore   = 3;

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetMatchDetailQuery(match.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("PENALTY_SHOOTOUT", result.MatchDuration);
        Assert.Equal(2, result.ExtraTimeHomeScore);
        Assert.Equal(2, result.ExtraTimeAwayScore);
        Assert.Equal(4, result.PenaltyHomeScore);
        Assert.Equal(3, result.PenaltyAwayScore);
    }
}
