using Bogus;
using WorldCuppy.Domain;
using WorldCuppy.Features.Predictions;
using WorldCuppy.Features.Sync;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Predictions;

/// <summary>Integration tests for <see cref="AwardPredictionPointsHandler" /> against a real PostgreSQL database.</summary>
public class AwardPredictionPointsHandlerTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    /// <summary>Creates a fresh handler instance with its own DbContext.</summary>
    private AwardPredictionPointsHandler CreateHandler() => new(db.CreateDbContext());

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

    private Match BuildMatch(Team home, Team away, int homeScore, int awayScore, MatchStatus status = MatchStatus.Finished) => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        HomeTeam   = home,
        HomeTeamId = home.Id,
        AwayTeam   = away,
        AwayTeamId = away.Id,
        KickoffUtc = new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero),
        GameDay    = new DateOnly(2026, 6, 15),
        Status     = status,
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
        Id                   = Guid.NewGuid(),
        UserId               = userId,
        MatchId              = match.Id,
        Match                = match,
        PredictedHomeScore   = predictedHome,
        PredictedAwayScore   = predictedAway,
        SubmittedAtUtc       = DateTimeOffset.UtcNow,
        PointsAwardedAtUtc   = null,
    };

    /// <summary>
    /// Multiple predictions on the same finished match should all receive correct points:
    /// exact scoreline gets 3 pts, correct result gets 1 pt.
    /// </summary>
    [Fact]
    public async Task Handle_WhenMultiplePredictionsExist_ShouldAwardCorrectPointsToAll()
    {
        // Arrange
        await using var ctx = db.CreateDbContext();

        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildMatch(home, away, homeScore: 2, awayScore: 1);

        var userA = BuildUser();
        var userB = BuildUser();

        // UserA predicts exact: 2-1 → 3 pts
        var predA = BuildPrediction(userA.Id, match, predictedHome: 2, predictedAway: 1);
        // UserB predicts correct result wrong score: 1-0 (home win) → 1 pt
        var predB = BuildPrediction(userB.Id, match, predictedHome: 1, predictedAway: 0);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.Users.AddRange(userA, userB);
        ctx.Predictions.AddRange(predA, predB);
        await ctx.SaveChangesAsync();

        var handler = CreateHandler();

        // Act
        await handler.Handle(new MatchFinishedEvent(match.Id, HomeScore: 2, AwayScore: 1), CancellationToken.None);

        // Assert
        await using var verify = db.CreateDbContext();

        var resultA = await verify.Predictions.FindAsync(predA.Id);
        var resultB = await verify.Predictions.FindAsync(predB.Id);

        Assert.NotNull(resultA);
        Assert.Equal(3, resultA.Points);
        Assert.NotNull(resultA.PointsAwardedAtUtc);

        Assert.NotNull(resultB);
        Assert.Equal(1, resultB.Points);
        Assert.NotNull(resultB.PointsAwardedAtUtc);
    }

    /// <summary>
    /// Publishing the same event twice should not overwrite already-awarded points —
    /// the second call is a no-op for predictions that already have PointsAwardedAtUtc set.
    /// </summary>
    [Fact]
    public async Task Handle_WhenEventPublishedTwice_ShouldNotOverwriteAwardedPoints()
    {
        // Arrange
        await using var ctx = db.CreateDbContext();

        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildMatch(home, away, homeScore: 3, awayScore: 0);

        var user = BuildUser();
        // Predicts correct result wrong score: 1-0 → 1 pt
        var pred = BuildPrediction(user.Id, match, predictedHome: 1, predictedAway: 0);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.Users.Add(user);
        ctx.Predictions.Add(pred);
        await ctx.SaveChangesAsync();

        var evt = new MatchFinishedEvent(match.Id, HomeScore: 3, AwayScore: 0);

        // Act — first publish
        await CreateHandler().Handle(evt, CancellationToken.None);

        await using var mid = db.CreateDbContext();
        var afterFirst = await mid.Predictions.FindAsync(pred.Id);
        Assert.NotNull(afterFirst);
        var firstTimestamp = afterFirst.PointsAwardedAtUtc;
        var firstPoints    = afterFirst.Points;

        // Act — second publish (should be a no-op)
        await CreateHandler().Handle(evt, CancellationToken.None);

        // Assert — points and timestamp unchanged
        await using var verify = db.CreateDbContext();
        var afterSecond = await verify.Predictions.FindAsync(pred.Id);
        Assert.NotNull(afterSecond);

        Assert.Equal(firstPoints,    afterSecond.Points);
        Assert.Equal(firstTimestamp, afterSecond.PointsAwardedAtUtc);
    }

    /// <summary>
    /// A match with no predictions should complete as a no-op without throwing.
    /// </summary>
    [Fact]
    public async Task Handle_WhenMatchHasNoPredictions_ShouldCompleteWithoutException()
    {
        // Arrange
        await using var ctx = db.CreateDbContext();

        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildMatch(home, away, homeScore: 1, awayScore: 0);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var handler = CreateHandler();

        // Act & Assert — should not throw
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new MatchFinishedEvent(match.Id, HomeScore: 1, AwayScore: 0), CancellationToken.None));

        Assert.Null(exception);
    }

    /// <summary>
    /// A group-stage match (no knockout round, group set) should award points
    /// using the same scoring rules as a knockout match.
    /// </summary>
    [Fact]
    public async Task Handle_WhenGroupStageMatchFinishes_ShouldAwardPointsSameAsKnockout()
    {
        // Arrange
        await using var ctx = db.CreateDbContext();

        var home  = BuildTeam();
        var away  = BuildTeam();

        // Group stage match: Round is null, Group is set
        var match = BuildMatch(home, away, homeScore: 1, awayScore: 1);
        match.Group = "GROUP_A";

        var user = BuildUser();
        // Predicts 0-0 — correct draw result but wrong score → 1 pt
        var pred = BuildPrediction(user.Id, match, predictedHome: 0, predictedAway: 0);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.Users.Add(user);
        ctx.Predictions.Add(pred);
        await ctx.SaveChangesAsync();

        var handler = CreateHandler();

        // Act
        await handler.Handle(new MatchFinishedEvent(match.Id, HomeScore: 1, AwayScore: 1), CancellationToken.None);

        // Assert
        await using var verify = db.CreateDbContext();
        var result = await verify.Predictions.FindAsync(pred.Id);

        Assert.NotNull(result);
        Assert.Equal(1, result.Points);
        Assert.NotNull(result.PointsAwardedAtUtc);
    }
}
