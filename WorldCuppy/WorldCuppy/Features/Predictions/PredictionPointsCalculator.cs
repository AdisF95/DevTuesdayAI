namespace WorldCuppy.Features.Predictions;

/// <summary>Pure scoring logic for a single prediction against an actual match result.</summary>
internal static class PredictionPointsCalculator
{
    /// <summary>
    /// Returns the points awarded for a single prediction.
    /// Exact scoreline = 3 pts, correct result outcome = 1 pt, wrong result = 0 pts.
    /// </summary>
    /// <param name="predictedHome">Predicted home team score.</param>
    /// <param name="predictedAway">Predicted away team score.</param>
    /// <param name="actualHome">Actual home team score.</param>
    /// <param name="actualAway">Actual away team score.</param>
    internal static int Calculate(
        int predictedHome,
        int predictedAway,
        int actualHome,
        int actualAway)
    {
        if (predictedHome == actualHome && predictedAway == actualAway)
        {
            return 3;
        }

        var predictedResult = Math.Sign(predictedHome - predictedAway);
        var actualResult    = Math.Sign(actualHome    - actualAway);

        return predictedResult == actualResult ? 1 : 0;
    }
}
