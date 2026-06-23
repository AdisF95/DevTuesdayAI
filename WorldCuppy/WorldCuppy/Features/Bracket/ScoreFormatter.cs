using WorldCuppy.Domain;

namespace WorldCuppy.Features.Bracket;

/// <summary>Formats a match score string for display in the knockout bracket.</summary>
internal static class ScoreFormatter
{
    /// <summary>
    /// Returns null when the match is not Finished; otherwise the formatted score string.
    /// Uses an en-dash (U+2013) as the score separator.
    /// </summary>
    internal static string? Format(
        MatchStatus status,
        string? matchDuration,
        int? homeScore, int? awayScore,
        int? extraTimeHomeScore, int? extraTimeAwayScore,
        int? penaltyHomeScore, int? penaltyAwayScore)
    {
        if (status != MatchStatus.Finished)
        {
            return null;
        }

        var home = homeScore ?? 0;
        var away = awayScore ?? 0;

        if (matchDuration == "PENALTY_SHOOTOUT")
        {
            var penHome = penaltyHomeScore ?? 0;
            var penAway = penaltyAwayScore ?? 0;
            return $"{home}–{away} ({penHome}–{penAway} pens)";
        }

        if (matchDuration == "EXTRA_TIME")
        {
            return $"{home}–{away} (aet)";
        }

        return $"{home}–{away}";
    }
}
