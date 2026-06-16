using Bogus;
using WorldCuppy.Features.Leaderboard;

namespace WorldCuppy.Tests.Unit.Leaderboard;

/// <summary>
/// Unit tests for <see cref="LeaderboardCalculator" />.
/// These are pure in-memory calculations — no database required.
/// </summary>
public class LeaderboardCalculatorTests
{
    private readonly Faker _faker = new();

    private LeaderboardCalculator.TeamEntry Team(string? name = null, string? code = null) =>
        new(
            Id: Guid.NewGuid(),
            Name: name ?? _faker.Address.Country(),
            Code: code ?? _faker.Random.String2(3).ToUpperInvariant());

    private static LeaderboardCalculator.MatchResult Match(
        Guid homeId, Guid awayId, int homeScore, int awayScore) =>
        new(homeId, awayId, homeScore, awayScore);

    // ── Ranking ───────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_WhenTeamsHaveNoMatches_ShouldRankAlphabetically()
    {
        var teamA = Team("Zebra FC");
        var teamB = Team("Alpha FC");

        var result = LeaderboardCalculator.Calculate([teamA, teamB], []);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha FC", result[0].Team);
        Assert.Equal("Zebra FC", result[1].Team);
    }

    [Fact]
    public void Calculate_WhenTeamHasMorePoints_ShouldBeRankedFirst()
    {
        var winner = Team("Winner FC");
        var loser = Team("Loser FC");

        var matches = new[]
        {
            Match(winner.Id, loser.Id, 2, 0),
        };

        var result = LeaderboardCalculator.Calculate([winner, loser], matches);

        Assert.Equal("Winner FC", result[0].Team);
        Assert.Equal(3, result[0].Points);
        Assert.Equal(0, result[1].Points);
    }

    [Fact]
    public void Calculate_WhenPointsAreEqual_ShouldUseGoalDifferenceToBreakTie()
    {
        var teamA = Team("Team A");
        var teamB = Team("Team B");
        var opponent = Team("Opponent");

        var matches = new[]
        {
            Match(teamA.Id, opponent.Id, 3, 0), // GD +3
            Match(teamB.Id, opponent.Id, 1, 0), // GD +1
        };

        var result = LeaderboardCalculator.Calculate([teamA, teamB, opponent], matches);

        Assert.Equal("Team A", result[0].Team);
        Assert.Equal("Team B", result[1].Team);
    }

    [Fact]
    public void Calculate_WhenPointsAndGoalDifferenceAreEqual_ShouldUseGoalsForToBreakTie()
    {
        var teamA = Team("Team A");
        var teamB = Team("Team B");
        var oppA = Team("Opp A");
        var oppB = Team("Opp B");

        var matches = new[]
        {
            Match(teamA.Id, oppA.Id, 5, 4), // GD +1, GF 5
            Match(teamB.Id, oppB.Id, 2, 1), // GD +1, GF 2
        };

        var result = LeaderboardCalculator.Calculate([teamA, teamB, oppA, oppB], matches);

        Assert.Equal("Team A", result[0].Team);
    }

    // ── Points calculation ────────────────────────────────────────────────────

    [Fact]
    public void Calculate_WhenTeamWinsAsHome_ShouldAward3Points()
    {
        var team = Team("England");
        var opp = Team("Opponent");

        var result = LeaderboardCalculator.Calculate(
            [team, opp],
            [Match(team.Id, opp.Id, 2, 1)]);

        var entry = result.Single(r => r.Team == "England");
        Assert.Equal(3, entry.Points);
        Assert.Equal(1, entry.Won);
        Assert.Equal(0, entry.Drawn);
        Assert.Equal(0, entry.Lost);
    }

    [Fact]
    public void Calculate_WhenTeamWinsAsAway_ShouldAward3Points()
    {
        var team = Team("England");
        var opp = Team("Opponent");

        var result = LeaderboardCalculator.Calculate(
            [team, opp],
            [Match(opp.Id, team.Id, 0, 1)]);

        var entry = result.Single(r => r.Team == "England");
        Assert.Equal(3, entry.Points);
        Assert.Equal(1, entry.Won);
    }

    [Fact]
    public void Calculate_WhenMatchIsDraw_ShouldAward1PointToEachTeam()
    {
        var teamA = Team("Team A");
        var teamB = Team("Team B");

        var result = LeaderboardCalculator.Calculate(
            [teamA, teamB],
            [Match(teamA.Id, teamB.Id, 1, 1)]);

        var a = result.Single(r => r.Team == "Team A");
        var b = result.Single(r => r.Team == "Team B");
        Assert.Equal(1, a.Points);
        Assert.Equal(1, b.Points);
        Assert.Equal(1, a.Drawn);
        Assert.Equal(1, b.Drawn);
    }

    [Fact]
    public void Calculate_WhenTeamLoses_ShouldAward0Points()
    {
        var team = Team("England");
        var opp = Team("Opponent");

        var result = LeaderboardCalculator.Calculate(
            [team, opp],
            [Match(team.Id, opp.Id, 0, 2)]);

        var entry = result.Single(r => r.Team == "England");
        Assert.Equal(0, entry.Points);
        Assert.Equal(1, entry.Lost);
    }

    // ── Goals accumulation ────────────────────────────────────────────────────

    [Fact]
    public void Calculate_WhenTeamHasHomeAndAwayMatches_ShouldAccumulateGoalsForAndAgainst()
    {
        var team = Team("England");
        var opp1 = Team("Opponent 1");
        var opp2 = Team("Opponent 2");

        var matches = new[]
        {
            Match(team.Id, opp1.Id, 3, 1), // home: GF+3, GA+1
            Match(opp2.Id, team.Id, 0, 2), // away: GF+2, GA+0
        };

        var result = LeaderboardCalculator.Calculate([team, opp1, opp2], matches);

        var entry = result.Single(r => r.Team == "England");
        Assert.Equal(5, entry.GoalsFor);
        Assert.Equal(1, entry.GoalsAgainst);
        Assert.Equal(4, entry.GoalDifference);
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_WhenRanksAreAssigned_ShouldBeSequentialFromOne()
    {
        var teams = Enumerable.Range(1, 5)
            .Select(i => Team($"Team {i}"))
            .ToList();

        var result = LeaderboardCalculator.Calculate(teams, []);

        for (var i = 0; i < result.Count; i++)
        {
            Assert.Equal(i + 1, result[i].Rank);
        }
    }

    [Fact]
    public void Calculate_WhenInputIsEmpty_ShouldReturnEmptyList()
    {
        var result = LeaderboardCalculator.Calculate([], []);
        Assert.Empty(result);
    }

    [Fact]
    public void Calculate_WhenMatchesAreUnrelatedToTeam_ShouldBeIgnored()
    {
        var team = Team("England");
        var opp1 = Team("France");
        var opp2 = Team("Germany");

        var matches = new[]
        {
            Match(opp1.Id, opp2.Id, 2, 0),
        };

        var result = LeaderboardCalculator.Calculate([team, opp1, opp2], matches);

        var entry = result.Single(r => r.Team == "England");
        Assert.Equal(0, entry.Played);
        Assert.Equal(0, entry.Points);
    }
}
