using Bogus;
using WorldCuppy.Domain;
using WorldCuppy.Features.Leaderboard;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Leaderboard;

/// <summary>Integration tests for <see cref="GetUserLeaderboardHandler" /> against a real PostgreSQL database.</summary>
public class GetUserLeaderboardQueryTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    /// <summary>Instantiates the handler with a fresh DbContext from the fixture.</summary>
    private GetUserLeaderboardHandler CreateHandler() => new(db.CreateDbContext());

    /// <summary>Builds a unique username safe for the database (max 10 chars, no special chars).</summary>
    private string SafeUsername()
    {
        var name = _faker.Internet.UserName().Replace(".", "_").Replace("-", "_");
        return name[..Math.Min(10, name.Length)];
    }

    private Team BuildTeam() => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        Name       = _faker.Address.Country(),
        Code       = _faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    };

    private Match BuildFinishedMatch(Team home, Team away, int homeScore, int awayScore) => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        HomeTeam   = home,
        HomeTeamId = home.Id,
        AwayTeam   = away,
        AwayTeamId = away.Id,
        KickoffUtc = new DateTimeOffset(2026, 7, 15, 18, 0, 0, TimeSpan.Zero),
        GameDay    = new DateOnly(2026, 7, 15),
        Status     = MatchStatus.Finished,
        Venue      = _faker.Address.City(),
        HomeScore  = homeScore,
        AwayScore  = awayScore,
    };

    private User BuildUser() => new()
    {
        Id           = Guid.NewGuid(),
        Username     = SafeUsername(),
        Email        = _faker.Internet.Email(),
        PasswordHash = _faker.Random.Hash(),
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    private static Prediction BuildPrediction(Guid userId, Match match, int predictedHome, int predictedAway) => new()
    {
        Id                 = Guid.NewGuid(),
        UserId             = userId,
        MatchId            = match.Id,
        Match              = match,
        PredictedHomeScore = predictedHome,
        PredictedAwayScore = predictedAway,
        SubmittedAtUtc     = DateTimeOffset.UtcNow,
    };

    /// <summary>
    /// Seeds two users with predictions on a finished match where one user predicted the exact
    /// scoreline (3 pts) and the other predicted the correct result but wrong score (1 pt).
    /// Points are pre-persisted on the Prediction rows. Verifies the handler reads persisted points
    /// and returns a ranked list with accurate totals.
    /// </summary>
    [Fact]
    public async Task GetUserLeaderboard_WhenUsersHavePredictionsOnFinishedMatch_ShouldReturnRankedEntries()
    {
        // Arrange
        await using var ctx = db.CreateDbContext();

        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildFinishedMatch(home, away, homeScore: 2, awayScore: 1);

        var userA = BuildUser();
        var userB = BuildUser();

        // User A predicts exact scoreline: 2-1 → 3 pts (pre-persisted)
        var predA = BuildPrediction(userA.Id, match, predictedHome: 2, predictedAway: 1);
        predA.Points             = 3;
        predA.PointsAwardedAtUtc = DateTimeOffset.UtcNow;

        // User B predicts correct result but wrong score: 1-0 (home win) → 1 pt (pre-persisted)
        var predB = BuildPrediction(userB.Id, match, predictedHome: 1, predictedAway: 0);
        predB.Points             = 1;
        predB.PointsAwardedAtUtc = DateTimeOffset.UtcNow;

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.Users.AddRange(userA, userB);
        ctx.Predictions.AddRange(predA, predB);
        await ctx.SaveChangesAsync();

        // Act
        var result = await CreateHandler().Handle(new GetUserLeaderboardQuery(), CancellationToken.None);

        // Assert — filter to the users seeded in this test to isolate from any other data in the shared container
        var relevant = result
            .Where(e => e.Username == userA.Username || e.Username == userB.Username)
            .OrderBy(e => e.Rank)
            .ToList();

        Assert.Equal(2, relevant.Count);

        var first  = relevant[0];
        var second = relevant[1];

        Assert.Equal(userA.Username, first.Username);
        Assert.Equal(3,              first.TotalPoints);
        Assert.Equal(1,              first.ExactScores);
        Assert.Equal(0,              first.CorrectResults);
        Assert.Equal(1,              first.PredictionsCount);

        Assert.Equal(userB.Username, second.Username);
        Assert.Equal(1,              second.TotalPoints);
        Assert.Equal(0,              second.ExactScores);
        Assert.Equal(1,              second.CorrectResults);
        Assert.Equal(1,              second.PredictionsCount);
    }

    /// <summary>
    /// A registered user with no predictions must appear in the leaderboard with 0 points,
    /// not be excluded. The query performs a left join so all users are included.
    /// </summary>
    [Fact]
    public async Task GetUserLeaderboard_WhenUserHasNoPredictions_ShouldAppearWithZeroPoints()
    {
        // Arrange — seed a user with no predictions
        await using var ctx = db.CreateDbContext();

        var userWithNoPredictions = BuildUser();
        ctx.Users.Add(userWithNoPredictions);
        await ctx.SaveChangesAsync();

        // Act
        var result = await CreateHandler().Handle(new GetUserLeaderboardQuery(), CancellationToken.None);

        // Assert — the user with zero predictions must appear with 0 points
        var entry = result.SingleOrDefault(e => e.Username == userWithNoPredictions.Username);
        Assert.NotNull(entry);
        Assert.Equal(0, entry.TotalPoints);
        Assert.Equal(0, entry.PredictionsCount);
        Assert.Equal(0, entry.ExactScores);
        Assert.Equal(0, entry.CorrectResults);
    }

    /// <summary>
    /// Predictions without PointsAwardedAtUtc set (not yet awarded) must not contribute to
    /// the leaderboard totals — the handler reads only persisted Points values.
    /// </summary>
    [Fact]
    public async Task GetUserLeaderboard_WhenPredictionHasNoPointsAwarded_ShouldNotCountTowardTotals()
    {
        // Arrange
        await using var ctx = db.CreateDbContext();

        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildFinishedMatch(home, away, homeScore: 1, awayScore: 0);

        var user = BuildUser();

        // Prediction without awarded points — PointsAwardedAtUtc is null
        var pred = BuildPrediction(user.Id, match, predictedHome: 1, predictedAway: 0);
        // Points and PointsAwardedAtUtc are intentionally left null

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.Users.Add(user);
        ctx.Predictions.Add(pred);
        await ctx.SaveChangesAsync();

        // Act
        var result = await CreateHandler().Handle(new GetUserLeaderboardQuery(), CancellationToken.None);

        // Assert — user appears but with 0 totals since points haven't been awarded yet
        var entry = result.SingleOrDefault(e => e.Username == user.Username);
        Assert.NotNull(entry);
        Assert.Equal(0, entry.TotalPoints);
        Assert.Equal(0, entry.PredictionsCount);
    }
}
