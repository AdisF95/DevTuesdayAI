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
    /// Verifies the returned list is ranked correctly with accurate point totals.
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

        // User A predicts exact scoreline: 2-1 → 3 pts
        var predA = BuildPrediction(userA.Id, match, predictedHome: 2, predictedAway: 1);
        // User B predicts correct result but wrong score: 1-0 (home win) → 1 pt
        var predB = BuildPrediction(userB.Id, match, predictedHome: 1, predictedAway: 0);

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
        Assert.Equal(1,              first.Rank);

        Assert.Equal(userB.Username, second.Username);
        Assert.Equal(1,              second.TotalPoints);
        Assert.Equal(0,              second.ExactScores);
        Assert.Equal(1,              second.CorrectResults);
        Assert.Equal(1,              second.PredictionsCount);
        Assert.Equal(2,              second.Rank);
    }

    /// <summary>
    /// When no finished match predictions exist the handler must return an empty list.
    /// A user seeded with no predictions must not appear in the leaderboard.
    /// </summary>
    [Fact]
    public async Task GetUserLeaderboard_WhenNoPredictionsExist_ShouldNotIncludeUnpredictingUser()
    {
        // Arrange — seed a user with no predictions
        await using var ctx = db.CreateDbContext();

        var isolatedUser = BuildUser();
        ctx.Users.Add(isolatedUser);
        await ctx.SaveChangesAsync();

        // Act
        var result = await CreateHandler().Handle(new GetUserLeaderboardQuery(), CancellationToken.None);

        // Assert — the user with zero predictions must not appear in the leaderboard at all
        Assert.DoesNotContain(result, e => e.Username == isolatedUser.Username);
    }
}
