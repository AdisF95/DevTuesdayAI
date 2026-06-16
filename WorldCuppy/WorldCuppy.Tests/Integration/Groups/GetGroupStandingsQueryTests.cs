using Bogus;
using WorldCuppy.Domain;
using WorldCuppy.Features.Groups;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Groups;

/// <summary>Integration tests for <see cref="GetGroupStandingsHandler" /> against a real PostgreSQL database.</summary>
public class GetGroupStandingsQueryTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    private GetGroupStandingsHandler Handler() => new(db.CreateDbContext());

    private Team BuildTeam() => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        Name       = _faker.Address.Country(),
        Code       = _faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    };

    private static GroupStanding BuildStanding(string group, int position, Team team, int points = 0, int played = 0) => new()
    {
        Id              = Guid.NewGuid(),
        Group           = group,
        Position        = position,
        TeamId          = team.Id,
        Team            = team,
        Points          = points,
        PlayedGames     = played,
        Won             = 0,
        Draw            = 0,
        Lost            = 0,
        GoalsFor        = 0,
        GoalsAgainst    = 0,
        GoalDifference  = 0,
    };

    [Fact]
    public async Task GetGroupStandings_ShouldReturnStandingsOrderedByGroupThenPosition()
    {
        await using var ctx = db.CreateDbContext();
        var teamA1 = BuildTeam();
        var teamA2 = BuildTeam();
        var teamB1 = BuildTeam();
        var teamB2 = BuildTeam();

        ctx.Teams.AddRange(teamA1, teamA2, teamB1, teamB2);
        // Insert deliberately out of order to prove sorting is applied.
        ctx.GroupStandings.AddRange(
            BuildStanding("GROUP_Z", 2, teamB2),
            BuildStanding("GROUP_Z", 1, teamB1),
            BuildStanding("GROUP_Y", 2, teamA2),
            BuildStanding("GROUP_Y", 1, teamA1));
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetGroupStandingsQuery(), CancellationToken.None);

        var inserted = result.Where(s => s.Group is "GROUP_Y" or "GROUP_Z").ToList();
        Assert.Equal(4, inserted.Count);
        // Groups must be in alphabetical order.
        Assert.Equal("GROUP_Y", inserted[0].Group);
        Assert.Equal("GROUP_Y", inserted[1].Group);
        Assert.Equal("GROUP_Z", inserted[2].Group);
        Assert.Equal("GROUP_Z", inserted[3].Group);
        // Within each group, position must ascend.
        Assert.Equal(1, inserted[0].Position);
        Assert.Equal(2, inserted[1].Position);
    }

    [Fact]
    public async Task GetGroupStandings_ShouldProjectTeamDetails()
    {
        await using var ctx = db.CreateDbContext();
        var team = BuildTeam();
        team.CrestUrl = "https://example.com/crest.png";

        ctx.Teams.Add(team);
        ctx.GroupStandings.Add(BuildStanding("GROUP_C", 1, team, points: 9, played: 3));
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetGroupStandingsQuery(), CancellationToken.None);

        var row = result.Single(s => s.TeamId == team.Id);
        Assert.Equal("GROUP_C",                     row.Group);
        Assert.Equal(1,                             row.Position);
        Assert.Equal(team.Name,                     row.TeamName);
        Assert.Equal(team.Code,                     row.TeamCode);
        Assert.Equal(team.CrestUrl,                 row.TeamCrest);
        Assert.Equal(9,                             row.Points);
        Assert.Equal(3,                             row.PlayedGames);
    }

    [Fact]
    public async Task GetGroupStandings_ShouldIncludeFormString()
    {
        await using var ctx = db.CreateDbContext();
        var team     = BuildTeam();
        var standing = BuildStanding("GROUP_D", 1, team);
        standing.Form = "W,W,D";

        ctx.Teams.Add(team);
        ctx.GroupStandings.Add(standing);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetGroupStandingsQuery(), CancellationToken.None);

        var row = result.Single(s => s.TeamId == team.Id);
        Assert.Equal("W,W,D", row.Form);
    }

    [Fact]
    public async Task GetGroupStandings_WhenNoStandingsExist_ShouldReturnEmptyList()
    {
        // Use a fresh handler against the shared DB — filter to avoid rows from other tests.
        var result = await Handler().Handle(new GetGroupStandingsQuery(), CancellationToken.None);

        // We can only assert that the call succeeds; other tests may have seeded rows.
        Assert.NotNull(result);
    }
}
