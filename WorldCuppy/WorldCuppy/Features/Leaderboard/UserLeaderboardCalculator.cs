namespace WorldCuppy.Features.Leaderboard;

/// <summary>Pure in-memory scoring and ranking logic for the user prediction leaderboard.</summary>
internal static class UserLeaderboardCalculator
{
    /// <summary>
    /// Returns the points awarded for a single prediction against the actual scoreline.
    /// Exact scoreline = 3 pts, correct result (win/draw/loss outcome matches) = 1 pt, wrong result = 0 pts.
    /// </summary>
    internal static int CalculatePoints(
        int predictedHome,
        int predictedAway,
        int actualHome,
        int actualAway)
    {
        if (predictedHome == actualHome && predictedAway == actualAway)
        {
            return 3;
        }

        var predictedOutcome = Math.Sign(predictedHome - predictedAway);
        var actualOutcome = Math.Sign(actualHome - actualAway);

        if (predictedOutcome == actualOutcome)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Returns a string label describing how the prediction compared to the actual result:
    /// "Exact" for correct scoreline, "Correct" for correct result only, "Wrong" otherwise.
    /// </summary>
    internal static string CalculateOutcome(
        int predictedHome,
        int predictedAway,
        int actualHome,
        int actualAway)
    {
        if (predictedHome == actualHome && predictedAway == actualAway)
        {
            return "Exact";
        }

        var predictedOutcome = Math.Sign(predictedHome - predictedAway);
        var actualOutcome = Math.Sign(actualHome - actualAway);

        return predictedOutcome == actualOutcome ? "Correct" : "Wrong";
    }

    /// <summary>Input record representing a user's scored prediction, used for ranking.</summary>
    internal record ScoredPrediction(
        Guid UserId,
        string Username,
        int Points,
        bool IsExact,
        bool IsCorrect);

    /// <summary>
    /// Aggregates a flat list of scored predictions into ranked leaderboard entries.
    /// Ordering: total points desc, then username asc.
    /// Users with no matching <see cref="ScoredPrediction" /> are not included.
    /// </summary>
    internal static List<UserLeaderboardEntryResponse> Rank(
        IEnumerable<ScoredPrediction> scoredPredictions)
    {
        var grouped = scoredPredictions
            .GroupBy(p => new { p.UserId, p.Username })
            .Select(g => new
            {
                g.Key.Username,
                TotalPoints = g.Sum(p => p.Points),
                PredictionsCount = g.Count(),
                ExactScores = g.Count(p => p.IsExact),
                CorrectResults = g.Count(p => p.IsCorrect),
            })
            .OrderByDescending(e => e.TotalPoints)
            .ThenBy(e => e.Username)
            .ToList();

        return grouped
            .Select((e, index) => new UserLeaderboardEntryResponse(
                Rank: index + 1,
                Username: e.Username,
                TotalPoints: e.TotalPoints,
                PredictionsCount: e.PredictionsCount,
                ExactScores: e.ExactScores,
                CorrectResults: e.CorrectResults))
            .ToList();
    }
}
