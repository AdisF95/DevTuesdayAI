using Bogus;
using WorldCuppy.Domain;
using WorldCuppy.Features.Predictions;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Predictions;

/// <summary>Integration tests for <see cref="CreatePredictionHandler" /> against a real PostgreSQL database.</summary>
public class CreatePredictionCommandTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    /// <summary>Instantiates the handler with a fresh DbContext from the fixture.</summary>
    private CreatePredictionHandler Handler() => new(db.CreateDbContext());

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
        KickoffUtc = new DateTimeOffset(2045, 10, 1, 18, 0, 0, TimeSpan.Zero),
        GameDay    = new DateOnly(2045, 10, 1),
        Status     = status,
        Venue      = _faker.Address.City(),
    };

    [Fact]
    public async Task CreatePrediction_WhenMatchIsFinished_ShouldThrowInvalidOperationException()
    {
        await using var ctx = db.CreateDbContext();
        var home  = BuildTeam();
        var away  = BuildTeam();
        var match = BuildMatch(home, away, MatchStatus.Finished);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var cmd = new CreatePredictionCommand(Guid.NewGuid(), match.Id, 1, 0);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler().Handle(cmd, CancellationToken.None));
    }
}
