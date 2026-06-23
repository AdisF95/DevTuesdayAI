using Bogus;
using WorldCuppy.Domain;
using WorldCuppy.Features.Bracket;

namespace WorldCuppy.Tests.Unit.Bracket;

/// <summary>Unit tests for <see cref="ScoreFormatter" /> covering all Format() branches.</summary>
public class ScoreFormatterTests
{
    private readonly Faker _faker = new();

    // ── Non-Finished statuses → null ─────────────────────────────────────────

    /// <summary>Scheduled match must return null regardless of scores.</summary>
    [Fact]
    public void Format_WhenStatusIsScheduled_ShouldReturnNull()
    {
        var result = ScoreFormatter.Format(
            status: MatchStatus.Scheduled,
            matchDuration: "REGULAR",
            homeScore: 2, awayScore: 1,
            extraTimeHomeScore: null, extraTimeAwayScore: null,
            penaltyHomeScore: null, penaltyAwayScore: null);

        Assert.Null(result);
    }

    /// <summary>Live match must return null so no score is shown mid-game.</summary>
    [Fact]
    public void Format_WhenStatusIsLive_ShouldReturnNull()
    {
        var home = _faker.Random.Int(0, 5);
        var away = _faker.Random.Int(0, 5);

        var result = ScoreFormatter.Format(
            status: MatchStatus.Live,
            matchDuration: null,
            homeScore: home, awayScore: away,
            extraTimeHomeScore: null, extraTimeAwayScore: null,
            penaltyHomeScore: null, penaltyAwayScore: null);

        Assert.Null(result);
    }

    /// <summary>Postponed match must return null.</summary>
    [Fact]
    public void Format_WhenStatusIsPostponed_ShouldReturnNull()
    {
        var result = ScoreFormatter.Format(
            status: MatchStatus.Postponed,
            matchDuration: "REGULAR",
            homeScore: null, awayScore: null,
            extraTimeHomeScore: null, extraTimeAwayScore: null,
            penaltyHomeScore: null, penaltyAwayScore: null);

        Assert.Null(result);
    }

    /// <summary>Cancelled match must return null.</summary>
    [Fact]
    public void Format_WhenStatusIsCancelled_ShouldReturnNull()
    {
        var result = ScoreFormatter.Format(
            status: MatchStatus.Cancelled,
            matchDuration: null,
            homeScore: null, awayScore: null,
            extraTimeHomeScore: null, extraTimeAwayScore: null,
            penaltyHomeScore: null, penaltyAwayScore: null);

        Assert.Null(result);
    }

    // ── Finished + REGULAR ───────────────────────────────────────────────────

    /// <summary>Finished regular-time match must return plain "home–away" score.</summary>
    [Fact]
    public void Format_WhenFinishedAndRegularDuration_ShouldReturnPlainScore()
    {
        var result = ScoreFormatter.Format(
            status: MatchStatus.Finished,
            matchDuration: "REGULAR",
            homeScore: 2, awayScore: 1,
            extraTimeHomeScore: null, extraTimeAwayScore: null,
            penaltyHomeScore: null, penaltyAwayScore: null);

        Assert.Equal("2–1", result);
    }

    /// <summary>Finished match with null duration must fall through to the plain score branch.</summary>
    [Fact]
    public void Format_WhenFinishedAndNullDuration_ShouldReturnPlainScore()
    {
        var home = _faker.Random.Int(0, 9);
        var away = _faker.Random.Int(0, 9);

        var result = ScoreFormatter.Format(
            status: MatchStatus.Finished,
            matchDuration: null,
            homeScore: home, awayScore: away,
            extraTimeHomeScore: null, extraTimeAwayScore: null,
            penaltyHomeScore: null, penaltyAwayScore: null);

        Assert.Equal($"{home}–{away}", result);
    }

    // ── Finished + EXTRA_TIME ─────────────────────────────────────────────────

    /// <summary>Finished extra-time match must return score with "(aet)" annotation.</summary>
    [Fact]
    public void Format_WhenFinishedAndExtraTimeDuration_ShouldReturnScoreWithAet()
    {
        var home = _faker.Random.Int(0, 5);
        var away = _faker.Random.Int(0, 5);

        var result = ScoreFormatter.Format(
            status: MatchStatus.Finished,
            matchDuration: "EXTRA_TIME",
            homeScore: home, awayScore: away,
            extraTimeHomeScore: home, extraTimeAwayScore: away,
            penaltyHomeScore: null, penaltyAwayScore: null);

        Assert.Equal($"{home}–{away} (aet)", result);
    }

    // ── Finished + PENALTY_SHOOTOUT ───────────────────────────────────────────

    /// <summary>Finished penalty-shootout match must return score with "(X–Y pens)" annotation.</summary>
    [Fact]
    public void Format_WhenFinishedAndPenaltyShootout_ShouldReturnScoreWithPens()
    {
        var result = ScoreFormatter.Format(
            status: MatchStatus.Finished,
            matchDuration: "PENALTY_SHOOTOUT",
            homeScore: 1, awayScore: 1,
            extraTimeHomeScore: 1, extraTimeAwayScore: 1,
            penaltyHomeScore: 4, penaltyAwayScore: 2);

        Assert.Equal("1–1 (4–2 pens)", result);
    }

    // ── En-dash separator ─────────────────────────────────────────────────────

    /// <summary>Score separator must be an en-dash (U+2013), not a hyphen-minus (U+002D).</summary>
    [Fact]
    public void Format_WhenFinishedAndRegular_ShouldUseEnDashSeparator()
    {
        var result = ScoreFormatter.Format(
            status: MatchStatus.Finished,
            matchDuration: "REGULAR",
            homeScore: 3, awayScore: 0,
            extraTimeHomeScore: null, extraTimeAwayScore: null,
            penaltyHomeScore: null, penaltyAwayScore: null);

        Assert.NotNull(result);
        // Verify en-dash U+2013 is present and hyphen-minus U+002D is not used as the separator.
        Assert.Contains('–', result);
        // The score must be "3–0" with en-dash, not "3-0" with hyphen.
        Assert.DoesNotContain("3-0", result);
    }
}
