using Bogus;
using WorldCuppy.Domain;
using WorldCuppy.Features.Predictions;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Predictions;

/// <summary>Integration tests for <see cref="UpdatePredictionHandler" /> against a real PostgreSQL database.</summary>
public class UpdatePredictionCommandTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    /// <summary>Instantiates the handler with a fresh DbContext from the fixture.</summary>
    private UpdatePredictionHandler Handler() => new(db.CreateDbContext());

    private Team BuildTeam() => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        Name       = _faker.Address.Country(),
        Code       = _faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    };

    private Match BuildMatch(Team home, Team away, MatchStatus status) => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        HomeTeam   = home,
        HomeTeamId = home.Id,
        AwayTeam   = away,
        AwayTeamId = away.Id,
        KickoffUtc = new DateTimeOffset(2045, 9, 1, 18, 0, 0, TimeSpan.Zero),
        GameDay    = new DateOnly(2045, 9, 1),
        Status     = status,
        Venue      = _faker.Address.City(),
    };

    private Prediction BuildPrediction(Guid userId, Match match) => new()
    {
        Id                 = Guid.NewGuid(),
        UserId             = userId,
        MatchId            = match.Id,
        Match              = match,
        PredictedHomeScore = _faker.Random.Int(0, 5),
        PredictedAwayScore = _faker.Random.Int(0, 5),
        SubmittedAtUtc     = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task UpdatePrediction_WhenValid_ShouldPersistUpdatedScoresAndReturnResponse()
    {
        // Arrange
        await using var ctx = db.CreateDbContext();
        var userId = Guid.NewGuid();
        var home   = BuildTeam();
        var away   = BuildTeam();
        var match  = BuildMatch(home, away, MatchStatus.Scheduled);
        var pred   = BuildPrediction(userId, match);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.Predictions.Add(pred);
        await ctx.SaveChangesAsync();

        var cmd = new UpdatePredictionCommand(pred.Id, userId, 3, 1);

        // Act
        var response = await Handler().Handle(cmd, CancellationToken.None);

        // Assert — returned DTO
        Assert.Equal(pred.Id,  response.Id);
        Assert.Equal(userId,   response.UserId);
        Assert.Equal(match.Id, response.MatchId);
        Assert.Equal(3, response.PredictedHomeScore);
        Assert.Equal(1, response.PredictedAwayScore);

        // Assert — persisted in DB
        await using var verify = db.CreateDbContext();
        var inDb = await verify.Predictions.FindAsync(pred.Id);
        Assert.NotNull(inDb);
        Assert.Equal(3, inDb.PredictedHomeScore);
        Assert.Equal(1, inDb.PredictedAwayScore);
    }

    [Fact]
    public async Task UpdatePrediction_WhenWrongOwner_ShouldThrowKeyNotFoundException()
    {
        await using var ctx = db.CreateDbContext();
        var realOwner  = Guid.NewGuid();
        var otherUser  = Guid.NewGuid();
        var home       = BuildTeam();
        var away       = BuildTeam();
        var match      = BuildMatch(home, away, MatchStatus.Scheduled);
        var pred       = BuildPrediction(realOwner, match);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.Predictions.Add(pred);
        await ctx.SaveChangesAsync();

        var cmd = new UpdatePredictionCommand(pred.Id, otherUser, 1, 0);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => Handler().Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePrediction_WhenMatchIsFinished_ShouldThrowInvalidOperationException()
    {
        await using var ctx = db.CreateDbContext();
        var userId = Guid.NewGuid();
        var home   = BuildTeam();
        var away   = BuildTeam();
        var match  = BuildMatch(home, away, MatchStatus.Finished);
        var pred   = BuildPrediction(userId, match);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        ctx.Predictions.Add(pred);
        await ctx.SaveChangesAsync();

        var cmd = new UpdatePredictionCommand(pred.Id, userId, 2, 2);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler().Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePrediction_WhenPredictionNotFound_ShouldThrowKeyNotFoundException()
    {
        var cmd = new UpdatePredictionCommand(Guid.NewGuid(), Guid.NewGuid(), 1, 1);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => Handler().Handle(cmd, CancellationToken.None));
    }
}
