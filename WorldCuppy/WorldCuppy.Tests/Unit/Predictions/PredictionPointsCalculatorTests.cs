using WorldCuppy.Features.Predictions;

namespace WorldCuppy.Tests.Unit.Predictions;

/// <summary>Unit tests for <see cref="PredictionPointsCalculator" />.</summary>
public class PredictionPointsCalculatorTests
{
    /// <summary>
    /// Exact scoreline prediction should award 3 points.
    /// </summary>
    [Fact]
    public void Calculate_WhenScorelineIsExact_ShouldAward3Points()
    {
        var result = PredictionPointsCalculator.Calculate(
            predictedHome: 2, predictedAway: 1,
            actualHome:    2, actualAway:    1);

        Assert.Equal(3, result);
    }

    /// <summary>
    /// Correct result with wrong score (home win predicted and actual) should award 1 point.
    /// </summary>
    [Fact]
    public void Calculate_WhenCorrectResultHomeWinWrongScore_ShouldAward1Point()
    {
        var result = PredictionPointsCalculator.Calculate(
            predictedHome: 1, predictedAway: 0,
            actualHome:    3, actualAway:    1);

        Assert.Equal(1, result);
    }

    /// <summary>
    /// Correct result with wrong score (away win predicted and actual) should award 1 point.
    /// </summary>
    [Fact]
    public void Calculate_WhenCorrectResultAwayWinWrongScore_ShouldAward1Point()
    {
        var result = PredictionPointsCalculator.Calculate(
            predictedHome: 0, predictedAway: 1,
            actualHome:    1, actualAway:    2);

        Assert.Equal(1, result);
    }

    /// <summary>
    /// Correct result with wrong score (draw predicted and actual) should award 1 point.
    /// </summary>
    [Fact]
    public void Calculate_WhenCorrectResultDrawWrongScore_ShouldAward1Point()
    {
        var result = PredictionPointsCalculator.Calculate(
            predictedHome: 1, predictedAway: 1,
            actualHome:    0, actualAway:    0);

        Assert.Equal(1, result);
    }

    /// <summary>
    /// Wrong result (predicted home win, actual away win) should award 0 points.
    /// </summary>
    [Fact]
    public void Calculate_WhenResultIsWrong_ShouldAward0Points()
    {
        var result = PredictionPointsCalculator.Calculate(
            predictedHome: 2, predictedAway: 0,
            actualHome:    0, actualAway:    1);

        Assert.Equal(0, result);
    }
}
