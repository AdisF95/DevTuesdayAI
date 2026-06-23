using Bogus;
using WorldCuppy.Features.Leaderboard;

namespace WorldCuppy.Tests.Unit.Leaderboard;

/// <summary>
/// Unit tests for <see cref="UserLeaderboardCalculator" />.
/// These are pure in-memory calculations — no database required.
/// </summary>
public class UserLeaderboardCalculatorTests
{
    private readonly Faker _faker = new();

    /// <summary>Creates a <see cref="UserLeaderboardCalculator.ScoredPrediction" /> with Bogus-generated identity fields.</summary>
    private UserLeaderboardCalculator.ScoredPrediction Scored(
        string? username = null,
        int points = 0,
        bool isExact = false,
        bool isCorrect = false) =>
        new(
            UserId: Guid.NewGuid(),
            Username: username ?? _faker.Internet.UserName(),
            Points: points,
            IsExact: isExact,
            IsCorrect: isCorrect);

    // ── CalculatePoints ───────────────────────────────────────────────────────

    /// <summary>Exact scoreline match must award 3 points.</summary>
    [Fact]
    public void CalculatePoints_WhenExactScoreline_ShouldReturn3Points()
    {
        var result = UserLeaderboardCalculator.CalculatePoints(
            predictedHome: 2, predictedAway: 1,
            actualHome: 2,    actualAway: 1);

        Assert.Equal(3, result);
    }

    /// <summary>Correct home-win outcome but different scoreline must award 1 point.</summary>
    [Fact]
    public void CalculatePoints_WhenCorrectResultButWrongScore_HomeWin_ShouldReturn1Point()
    {
        var result = UserLeaderboardCalculator.CalculatePoints(
            predictedHome: 2, predictedAway: 1,
            actualHome: 3,    actualAway: 0);

        Assert.Equal(1, result);
    }

    /// <summary>Correct draw outcome but different scoreline must award 1 point.</summary>
    [Fact]
    public void CalculatePoints_WhenCorrectResultButWrongScore_Draw_ShouldReturn1Point()
    {
        var result = UserLeaderboardCalculator.CalculatePoints(
            predictedHome: 1, predictedAway: 1,
            actualHome: 0,    actualAway: 0);

        Assert.Equal(1, result);
    }

    /// <summary>Wrong outcome (predicted home win, actual away win) must award 0 points.</summary>
    [Fact]
    public void CalculatePoints_WhenWrongResult_ShouldReturn0Points()
    {
        var result = UserLeaderboardCalculator.CalculatePoints(
            predictedHome: 2, predictedAway: 1,
            actualHome: 0,    actualAway: 1);

        Assert.Equal(0, result);
    }

    // ── Rank ─────────────────────────────────────────────────────────────────

    /// <summary>User with more total points must appear before user with fewer points.</summary>
    [Fact]
    public void Rank_WhenUsersHaveDifferentPoints_ShouldOrderByPointsDescending()
    {
        var highScorer = Scored(username: _faker.Internet.UserName(), points: 3, isExact: true);
        var lowScorer  = Scored(username: _faker.Internet.UserName(), points: 1, isCorrect: true);

        var result = UserLeaderboardCalculator.Rank([highScorer, lowScorer]);

        Assert.Equal(2, result.Count);
        Assert.Equal(highScorer.Username, result[0].Username);
        Assert.Equal(lowScorer.Username, result[1].Username);
        Assert.Equal(3, result[0].TotalPoints);
        Assert.Equal(1, result[1].TotalPoints);
    }

    /// <summary>When two users have equal points, the one whose username sorts first alphabetically must rank higher.</summary>
    [Fact]
    public void Rank_WhenPointsTied_ShouldBreakTieByUsernameAscending()
    {
        var userZebra = Scored(username: "zebra_user", points: 1, isCorrect: true);
        var userAlpha = Scored(username: "alpha_user", points: 1, isCorrect: true);

        var result = UserLeaderboardCalculator.Rank([userZebra, userAlpha]);

        Assert.Equal(2, result.Count);
        Assert.Equal("alpha_user", result[0].Username);
        Assert.Equal("zebra_user", result[1].Username);
    }

    /// <summary>A single user in the list must receive rank 1.</summary>
    [Fact]
    public void Rank_WhenSingleUser_ShouldAssignRank1()
    {
        var entry = Scored(points: 3, isExact: true);

        var result = UserLeaderboardCalculator.Rank([entry]);

        Assert.Single(result);
        Assert.Equal(1, result[0].Rank);
    }

    /// <summary>An empty input collection must produce an empty leaderboard.</summary>
    [Fact]
    public void Rank_WhenEmpty_ShouldReturnEmptyList()
    {
        var result = UserLeaderboardCalculator.Rank([]);

        Assert.Empty(result);
    }

    /// <summary>Multiple predictions for the same user must be aggregated into a single entry with correct totals.</summary>
    [Fact]
    public void Rank_WhenUserHasMultiplePredictions_ShouldAggregateCorrectly()
    {
        var userId   = Guid.NewGuid();
        var username = _faker.Internet.UserName();

        var pred1 = new UserLeaderboardCalculator.ScoredPrediction(userId, username, 3, IsExact: true,  IsCorrect: false);
        var pred2 = new UserLeaderboardCalculator.ScoredPrediction(userId, username, 1, IsExact: false, IsCorrect: true);
        var pred3 = new UserLeaderboardCalculator.ScoredPrediction(userId, username, 0, IsExact: false, IsCorrect: false);

        var result = UserLeaderboardCalculator.Rank([pred1, pred2, pred3]);

        Assert.Single(result);
        var entry = result[0];
        Assert.Equal(4,  entry.TotalPoints);
        Assert.Equal(3,  entry.PredictionsCount);
        Assert.Equal(1,  entry.ExactScores);
        Assert.Equal(1,  entry.CorrectResults);
        Assert.Equal(1,  entry.Rank);
    }

    /// <summary>Sequential ranks starting at 1 must be assigned in order of position after sorting.</summary>
    [Fact]
    public void Rank_WhenMultipleUsers_ShouldAssignSequentialRanks()
    {
        var entries = Enumerable.Range(1, 4)
            .Select(i => Scored(username: $"user_{i:D2}", points: i))
            .ToList();

        var result = UserLeaderboardCalculator.Rank(entries);

        for (var i = 0; i < result.Count; i++)
        {
            Assert.Equal(i + 1, result[i].Rank);
        }
    }
}
