using WorldCuppy.Domain;
using WorldCuppy.Features.Sync;

namespace WorldCuppy.Tests.Unit.Sync;

/// <summary>
/// Unit tests for <see cref="SyncHandler" />'s internal status and round mapping methods.
/// These are pure switch expressions — no database or HTTP dependencies.
/// </summary>
public class SyncHandlerMappingTests
{
    // ── MapStatus ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("IN_PLAY",   MatchStatus.Live)]
    [InlineData("PAUSED",    MatchStatus.Live)]
    [InlineData("SUSPENDED", MatchStatus.Live)]
    public void MapStatus_WhenStatusIsInPlay_ShouldReturnLive(string input, MatchStatus expected)
    {
        Assert.Equal(expected, SyncHandler.MapStatus(input));
    }

    [Theory]
    [InlineData("FINISHED", MatchStatus.Finished)]
    [InlineData("AWARDED",  MatchStatus.Finished)]
    public void MapStatus_WhenStatusIsFinished_ShouldReturnFinished(string input, MatchStatus expected)
    {
        Assert.Equal(expected, SyncHandler.MapStatus(input));
    }

    [Fact]
    public void MapStatus_WhenStatusIsPostponed_ShouldReturnPostponed()
    {
        Assert.Equal(MatchStatus.Postponed, SyncHandler.MapStatus("POSTPONED"));
    }

    [Fact]
    public void MapStatus_WhenStatusIsCancelled_ShouldReturnCancelled()
    {
        Assert.Equal(MatchStatus.Cancelled, SyncHandler.MapStatus("CANCELLED"));
    }

    [Theory]
    [InlineData("SCHEDULED")]
    [InlineData("TIMED")]
    [InlineData("UNKNOWN")]
    [InlineData("")]
    public void MapStatus_WhenStatusIsUnknownOrDefault_ShouldReturnScheduled(string input)
    {
        Assert.Equal(MatchStatus.Scheduled, SyncHandler.MapStatus(input));
    }

    // ── MapRound ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("LAST_32",     KnockoutRound.RoundOf32)]
    [InlineData("ROUND_OF_32", KnockoutRound.RoundOf32)]
    public void MapRound_WhenStageIsRoundOf32_ShouldReturnRoundOf32(string input, KnockoutRound expected)
    {
        Assert.Equal(expected, SyncHandler.MapRound(input));
    }

    [Theory]
    [InlineData("LAST_16",     KnockoutRound.RoundOf16)]
    [InlineData("ROUND_OF_16", KnockoutRound.RoundOf16)]
    public void MapRound_WhenStageIsRoundOf16_ShouldReturnRoundOf16(string input, KnockoutRound expected)
    {
        Assert.Equal(expected, SyncHandler.MapRound(input));
    }

    [Fact]
    public void MapRound_WhenStageIsQuarterFinals_ShouldReturnQuarterFinal()
    {
        Assert.Equal(KnockoutRound.QuarterFinal, SyncHandler.MapRound("QUARTER_FINALS"));
    }

    [Fact]
    public void MapRound_WhenStageIsSemiFinals_ShouldReturnSemiFinal()
    {
        Assert.Equal(KnockoutRound.SemiFinal, SyncHandler.MapRound("SEMI_FINALS"));
    }

    [Fact]
    public void MapRound_WhenStageIsFinal_ShouldReturnFinal()
    {
        Assert.Equal(KnockoutRound.Final, SyncHandler.MapRound("FINAL"));
    }

    [Theory]
    [InlineData("GROUP_STAGE")]
    [InlineData("UNKNOWN_STAGE")]
    [InlineData("")]
    public void MapRound_WhenStageIsGroupStageOrUnknown_ShouldReturnNull(string input)
    {
        Assert.Null(SyncHandler.MapRound(input));
    }
}
