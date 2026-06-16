using Bogus;
using WorldCuppy.Domain;
using WorldCuppy.Features.Matches;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Matches;

/// <summary>Integration tests for <see cref="GetAllMatchesHandler" /> against a real PostgreSQL database.</summary>
public class GetAllMatchesQueryTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    private GetAllMatchesHandler Handler() => new(db.CreateDbContext());

    private Team BuildTeam() => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        Name       = _faker.Address.Country(),
        Code       = _faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    };

    private Match BuildMatch(Team home, Team away, DateTimeOffset kickoff) => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        HomeTeam   = home,
        HomeTeamId = home.Id,
        AwayTeam   = away,
        AwayTeamId = away.Id,
        KickoffUtc = kickoff,
        GameDay    = DateOnly.FromDateTime(kickoff.UtcDateTime),
        Status     = MatchStatus.Scheduled,
        Venue      = _faker.Address.City(),
        Group      = "GROUP_A",
    };

    [Fact]
    public async Task GetAllMatches_ShouldReturnMatchesOrderedByKickoff()
    {
        await using var ctx = db.CreateDbContext();

        var home = BuildTeam();
        var away = BuildTeam();

        // Insert in reverse chronological order to prove sorting is applied.
        var later   = new DateTimeOffset(2040, 1, 2, 20, 0, 0, TimeSpan.Zero);
        var earlier = new DateTimeOffset(2040, 1, 1, 18, 0, 0, TimeSpan.Zero);

        var matchLater   = BuildMatch(home, away, later);
        var matchEarlier = BuildMatch(away, home, earlier);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.AddRange(matchLater, matchEarlier);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetAllMatchesQuery(), CancellationToken.None);

        var inserted = result
            .Where(m => m.Id == matchEarlier.Id || m.Id == matchLater.Id)
            .ToList();

        Assert.Equal(2, inserted.Count);
        Assert.True(inserted[0].KickoffUtc <= inserted[1].KickoffUtc);
    }

    [Fact]
    public async Task GetAllMatches_ShouldIncludeHomeAndAwayTeamDetails()
    {
        await using var ctx = db.CreateDbContext();

        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildMatch(home, away, new DateTimeOffset(2040, 2, 1, 15, 0, 0, TimeSpan.Zero));

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetAllMatchesQuery(), CancellationToken.None);

        var found = result.Single(m => m.Id == match.Id);
        Assert.Equal(home.Name, found.HomeTeam);
        Assert.Equal(home.Code, found.HomeTeamCode);
        Assert.Equal(away.Name, found.AwayTeam);
        Assert.Equal(away.Code, found.AwayTeamCode);
    }

    [Fact]
    public async Task GetAllMatches_WhenMatchesSpanMultipleDays_ShouldReturnAll()
    {
        await using var ctx = db.CreateDbContext();

        var teamA = BuildTeam();
        var teamB = BuildTeam();

        var day1 = new DateTimeOffset(2040, 3, 1, 18, 0, 0, TimeSpan.Zero);
        var day2 = new DateTimeOffset(2040, 3, 2, 20, 0, 0, TimeSpan.Zero);

        var match1 = BuildMatch(teamA, teamB, day1);
        var match2 = BuildMatch(teamB, teamA, day2);

        ctx.Teams.AddRange(teamA, teamB);
        ctx.Matches.AddRange(match1, match2);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetAllMatchesQuery(), CancellationToken.None);

        Assert.Contains(result, m => m.Id == match1.Id && m.GameDay == DateOnly.FromDateTime(day1.UtcDateTime));
        Assert.Contains(result, m => m.Id == match2.Id && m.GameDay == DateOnly.FromDateTime(day2.UtcDateTime));
    }
}
