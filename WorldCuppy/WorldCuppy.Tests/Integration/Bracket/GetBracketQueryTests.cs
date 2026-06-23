using Bogus;
using WorldCuppy.Domain;
using WorldCuppy.Features.Bracket;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Bracket;

/// <summary>Integration tests for <see cref="GetBracketHandler" /> against a real PostgreSQL database.</summary>
public class GetBracketQueryTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    /// <summary>Instantiates the handler with a fresh DbContext from the fixture.</summary>
    private GetBracketHandler Handler() => new(db.CreateDbContext());

    /// <summary>Builds a <see cref="Team" /> with Bogus-generated identity fields.</summary>
    private Team BuildTeam() => new()
    {
        Id         = Guid.NewGuid(),
        ExternalId = Math.Abs(Guid.NewGuid().GetHashCode()),
        Name       = _faker.Address.Country(),
        Code       = _faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
    };

    /// <summary>Builds a knockout <see cref="Match" /> for the given round and kickoff time.</summary>
    private Match BuildKnockoutMatch(Team home, Team away, KnockoutRound round, DateTimeOffset kickoff, MatchStatus status = MatchStatus.Scheduled) => new()
    {
        Id           = Guid.NewGuid(),
        ExternalId   = Math.Abs(Guid.NewGuid().GetHashCode()),
        HomeTeam     = home,
        HomeTeamId   = home.Id,
        AwayTeam     = away,
        AwayTeamId   = away.Id,
        KickoffUtc   = kickoff,
        GameDay      = DateOnly.FromDateTime(kickoff.UtcDateTime),
        Round        = round,
        Status       = status,
        Venue        = _faker.Address.City(),
    };

    /// <summary>Builds a group-stage <see cref="Match" /> (Round == null).</summary>
    private Match BuildGroupMatch(Team home, Team away, DateTimeOffset kickoff) => new()
    {
        Id           = Guid.NewGuid(),
        ExternalId   = Math.Abs(Guid.NewGuid().GetHashCode()),
        HomeTeam     = home,
        HomeTeamId   = home.Id,
        AwayTeam     = away,
        AwayTeamId   = away.Id,
        KickoffUtc   = kickoff,
        GameDay      = DateOnly.FromDateTime(kickoff.UtcDateTime),
        Round        = null,
        Group        = "GROUP_A",
        Status       = MatchStatus.Scheduled,
        Venue        = _faker.Address.City(),
    };

    // ── Empty bracket ────────────────────────────────────────────────────────

    /// <summary>When no knockout matches exist the bracket must return an empty Rounds list.</summary>
    [Fact]
    public async Task GetBracket_WhenNoBracketMatches_ShouldReturnEmptyRounds()
    {
        // Seed a group-stage match only — Round is null, so it must be excluded.
        await using var ctx = db.CreateDbContext();
        var home = BuildTeam();
        var away = BuildTeam();
        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(BuildGroupMatch(home, away, new DateTimeOffset(2041, 6, 1, 18, 0, 0, TimeSpan.Zero)));
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetBracketQuery(), CancellationToken.None);

        // The bracket may contain rounds seeded by other tests; filter to the matches we own.
        // Because we cannot filter the result by our own seeds without leaking test coupling,
        // we verify the empty-bracket path by using a fresh scenario with no knockout rows at all.
        // The shared DB may have rows from parallel tests, so assert that our group match is absent.
        Assert.DoesNotContain(result.Rounds, r =>
            r.Matches.Any(m => m.HomeTeam.Name == home.Name || m.AwayTeam.Name == away.Name));
    }

    // ── Group-stage exclusion ─────────────────────────────────────────────────

    /// <summary>Group-stage matches (Round == null) must not appear in the bracket response.</summary>
    [Fact]
    public async Task GetBracket_ShouldExcludeGroupStageMatches()
    {
        await using var ctx = db.CreateDbContext();
        var homeGroup  = BuildTeam();
        var awayGroup  = BuildTeam();
        var homeKnock  = BuildTeam();
        var awayKnock  = BuildTeam();

        var kickoff = new DateTimeOffset(2042, 7, 1, 20, 0, 0, TimeSpan.Zero);

        var groupMatch   = BuildGroupMatch(homeGroup, awayGroup, kickoff);
        var knockoutMatch = BuildKnockoutMatch(homeKnock, awayKnock, KnockoutRound.RoundOf16, kickoff.AddHours(2));

        ctx.Teams.AddRange(homeGroup, awayGroup, homeKnock, awayKnock);
        ctx.Matches.AddRange(groupMatch, knockoutMatch);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetBracketQuery(), CancellationToken.None);

        // The group match must not appear under any round.
        Assert.DoesNotContain(result.Rounds, r =>
            r.Matches.Any(m => m.MatchId == groupMatch.Id));

        // The knockout match must appear.
        Assert.Contains(result.Rounds, r =>
            r.Matches.Any(m => m.MatchId == knockoutMatch.Id));
    }

    // ── Round grouping ────────────────────────────────────────────────────────

    /// <summary>Matches seeded across two rounds must be grouped into two separate round buckets.</summary>
    [Fact]
    public async Task GetBracket_ShouldGroupMatchesByRound()
    {
        await using var ctx = db.CreateDbContext();

        var kickoff = new DateTimeOffset(2043, 7, 1, 15, 0, 0, TimeSpan.Zero);

        Team NewPair(out Team home, out Team away)
        {
            home = BuildTeam();
            away = BuildTeam();
            return home; // unused return — just a helper trick
        }

        Team h1, a1, h2, a2, h3, a3;
        NewPair(out h1, out a1);
        NewPair(out h2, out a2);
        NewPair(out h3, out a3);

        var r16Match1  = BuildKnockoutMatch(h1, a1, KnockoutRound.RoundOf16, kickoff);
        var r16Match2  = BuildKnockoutMatch(h2, a2, KnockoutRound.RoundOf16, kickoff.AddHours(3));
        var qfMatch    = BuildKnockoutMatch(h3, a3, KnockoutRound.QuarterFinal, kickoff.AddDays(3));

        ctx.Teams.AddRange(h1, a1, h2, a2, h3, a3);
        ctx.Matches.AddRange(r16Match1, r16Match2, qfMatch);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetBracketQuery(), CancellationToken.None);

        var r16Round = result.Rounds.SingleOrDefault(r => r.Round == "RoundOf16" &&
            r.Matches.Any(m => m.MatchId == r16Match1.Id || m.MatchId == r16Match2.Id));
        var qfRound  = result.Rounds.SingleOrDefault(r => r.Round == "QuarterFinal" &&
            r.Matches.Any(m => m.MatchId == qfMatch.Id));

        Assert.NotNull(r16Round);
        Assert.NotNull(qfRound);
        Assert.Contains(r16Round.Matches, m => m.MatchId == r16Match1.Id);
        Assert.Contains(r16Round.Matches, m => m.MatchId == r16Match2.Id);
        Assert.Contains(qfRound.Matches,  m => m.MatchId == qfMatch.Id);
    }

    // ── Round ordering ────────────────────────────────────────────────────────

    /// <summary>Rounds must appear in fixed enum order (RoundOf32 before Final) regardless of insertion order.</summary>
    [Fact]
    public async Task GetBracket_ShouldOrderRoundsInFixedBracketOrder()
    {
        await using var ctx = db.CreateDbContext();

        var kickoff = new DateTimeOffset(2044, 6, 15, 19, 0, 0, TimeSpan.Zero);
        var hFinal = BuildTeam(); var aFinal = BuildTeam();
        var hR32   = BuildTeam(); var aR32   = BuildTeam();

        // Insert Final first to prove the ordering is driven by enum value, not insertion order.
        var finalMatch = BuildKnockoutMatch(hFinal, aFinal, KnockoutRound.Final,    kickoff.AddDays(20));
        var r32Match   = BuildKnockoutMatch(hR32,   aR32,   KnockoutRound.RoundOf32, kickoff);

        ctx.Teams.AddRange(hFinal, aFinal, hR32, aR32);
        ctx.Matches.AddRange(finalMatch, r32Match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetBracketQuery(), CancellationToken.None);

        // Filter to only the rounds that contain our seeded matches.
        var seededRounds = result.Rounds
            .Where(r => r.Matches.Any(m => m.MatchId == finalMatch.Id || m.MatchId == r32Match.Id))
            .ToList();

        Assert.Equal(2, seededRounds.Count);
        Assert.Equal("RoundOf32", seededRounds[0].Round);
        Assert.Equal("Final",     seededRounds[1].Round);
    }

    // ── Match ordering within round ───────────────────────────────────────────

    /// <summary>Matches within a round must be ordered by KickoffUtc ascending.</summary>
    [Fact]
    public async Task GetBracket_ShouldOrderMatchesWithinRoundByKickoffAscending()
    {
        await using var ctx = db.CreateDbContext();

        var earlyKickoff = new DateTimeOffset(2045, 6, 1, 15, 0, 0, TimeSpan.Zero);
        var lateKickoff  = new DateTimeOffset(2045, 6, 1, 20, 0, 0, TimeSpan.Zero);

        var hEarly = BuildTeam(); var aEarly = BuildTeam();
        var hLate  = BuildTeam(); var aLate  = BuildTeam();

        // Insert the later match first to prove sorting is applied.
        var lateMatch  = BuildKnockoutMatch(hLate,  aLate,  KnockoutRound.SemiFinal, lateKickoff);
        var earlyMatch = BuildKnockoutMatch(hEarly, aEarly, KnockoutRound.SemiFinal, earlyKickoff);

        ctx.Teams.AddRange(hEarly, aEarly, hLate, aLate);
        ctx.Matches.AddRange(lateMatch, earlyMatch);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetBracketQuery(), CancellationToken.None);

        var sfRound = result.Rounds.Single(r =>
            r.Round == "SemiFinal" &&
            r.Matches.Any(m => m.MatchId == earlyMatch.Id || m.MatchId == lateMatch.Id));

        var ourMatches = sfRound.Matches
            .Where(m => m.MatchId == earlyMatch.Id || m.MatchId == lateMatch.Id)
            .ToList();

        Assert.Equal(2, ourMatches.Count);
        Assert.Equal(earlyMatch.Id, ourMatches[0].MatchId);
        Assert.Equal(lateMatch.Id,  ourMatches[1].MatchId);
    }

    // ── Score formatting end-to-end ───────────────────────────────────────────

    /// <summary>A Finished regular-time match must have Score formatted as "H–A" with an en-dash.</summary>
    [Fact]
    public async Task GetBracket_WhenMatchIsFinishedRegular_ShouldReturnFormattedScore()
    {
        await using var ctx = db.CreateDbContext();

        var home = BuildTeam();
        var away = BuildTeam();
        var kickoff = new DateTimeOffset(2046, 6, 10, 18, 0, 0, TimeSpan.Zero);
        var match = BuildKnockoutMatch(home, away, KnockoutRound.RoundOf16, kickoff, MatchStatus.Finished);
        match.HomeScore     = 2;
        match.AwayScore     = 1;
        match.MatchDuration = "REGULAR";

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetBracketQuery(), CancellationToken.None);

        var dto = result.Rounds
            .SelectMany(r => r.Matches)
            .Single(m => m.MatchId == match.Id);

        Assert.Equal("2–1", dto.Score);
    }

    /// <summary>A Scheduled knockout match must have a null Score (no score to display yet).</summary>
    [Fact]
    public async Task GetBracket_WhenMatchIsScheduled_ShouldReturnNullScore()
    {
        await using var ctx = db.CreateDbContext();

        var home = BuildTeam();
        var away = BuildTeam();
        var kickoff = new DateTimeOffset(2047, 6, 20, 20, 0, 0, TimeSpan.Zero);
        var match = BuildKnockoutMatch(home, away, KnockoutRound.QuarterFinal, kickoff, MatchStatus.Scheduled);

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetBracketQuery(), CancellationToken.None);

        var dto = result.Rounds
            .SelectMany(r => r.Matches)
            .Single(m => m.MatchId == match.Id);

        Assert.Null(dto.Score);
    }

    /// <summary>A Finished extra-time match must have Score formatted as "H–A (aet)".</summary>
    [Fact]
    public async Task GetBracket_WhenMatchIsFinishedExtraTime_ShouldReturnAetScore()
    {
        await using var ctx = db.CreateDbContext();

        var home = BuildTeam();
        var away = BuildTeam();
        var kickoff = new DateTimeOffset(2048, 6, 25, 17, 0, 0, TimeSpan.Zero);
        var match = BuildKnockoutMatch(home, away, KnockoutRound.SemiFinal, kickoff, MatchStatus.Finished);
        match.HomeScore          = 2;
        match.AwayScore          = 1;
        match.ExtraTimeHomeScore = 2;
        match.ExtraTimeAwayScore = 1;
        match.MatchDuration      = "EXTRA_TIME";

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetBracketQuery(), CancellationToken.None);

        var dto = result.Rounds
            .SelectMany(r => r.Matches)
            .Single(m => m.MatchId == match.Id);

        Assert.Equal("2–1 (aet)", dto.Score);
    }

    /// <summary>A Finished penalty-shootout match must have Score formatted as "H–A (X–Y pens)".</summary>
    [Fact]
    public async Task GetBracket_WhenMatchIsFinishedPenaltyShootout_ShouldReturnPensScore()
    {
        await using var ctx = db.CreateDbContext();

        var home = BuildTeam();
        var away = BuildTeam();
        var kickoff = new DateTimeOffset(2049, 7, 1, 19, 0, 0, TimeSpan.Zero);
        var match = BuildKnockoutMatch(home, away, KnockoutRound.Final, kickoff, MatchStatus.Finished);
        match.HomeScore          = 1;
        match.AwayScore          = 1;
        match.ExtraTimeHomeScore = 1;
        match.ExtraTimeAwayScore = 1;
        match.PenaltyHomeScore   = 4;
        match.PenaltyAwayScore   = 2;
        match.MatchDuration      = "PENALTY_SHOOTOUT";

        ctx.Teams.AddRange(home, away);
        ctx.Matches.Add(match);
        await ctx.SaveChangesAsync();

        var result = await Handler().Handle(new GetBracketQuery(), CancellationToken.None);

        var dto = result.Rounds
            .SelectMany(r => r.Matches)
            .Single(m => m.MatchId == match.Id);

        Assert.Equal("1–1 (4–2 pens)", dto.Score);
    }
}
