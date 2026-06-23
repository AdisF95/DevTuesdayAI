using Bogus;
using FluentValidation.TestHelper;
using WorldCuppy.Features.Predictions;

namespace WorldCuppy.Tests.Unit.Predictions;

/// <summary>Unit tests for <see cref="UpdatePredictionValidator" />.</summary>
public class UpdatePredictionValidatorTests
{
    private readonly UpdatePredictionValidator _validator = new();
    private readonly Faker _faker = new();

    /// <summary>A valid command built with Bogus — all rule variations start from this baseline.</summary>
    private UpdatePredictionCommand ValidCommand() => new(
        PredictionId: _faker.Random.Guid(),
        UserId: _faker.Random.Guid(),
        PredictedHomeScore: _faker.Random.Int(0, 20),
        PredictedAwayScore: _faker.Random.Int(0, 20));

    [Fact]
    public void UpdatePredictionValidator_WhenCommandIsValid_ShouldPassAllRules()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdatePredictionValidator_WhenPredictionIdIsEmpty_ShouldFailValidation()
    {
        var cmd = ValidCommand() with { PredictionId = Guid.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PredictionId);
    }

    [Fact]
    public void UpdatePredictionValidator_WhenUserIdIsEmpty_ShouldFailValidation()
    {
        var cmd = ValidCommand() with { UserId = Guid.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void UpdatePredictionValidator_WhenHomeScoreIsNegative_ShouldFailValidation(int score)
    {
        var cmd = ValidCommand() with { PredictedHomeScore = score };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PredictedHomeScore);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(100)]
    public void UpdatePredictionValidator_WhenHomeScoreExceedsMaximum_ShouldFailValidation(int score)
    {
        var cmd = ValidCommand() with { PredictedHomeScore = score };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PredictedHomeScore);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void UpdatePredictionValidator_WhenAwayScoreIsNegative_ShouldFailValidation(int score)
    {
        var cmd = ValidCommand() with { PredictedAwayScore = score };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PredictedAwayScore);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(100)]
    public void UpdatePredictionValidator_WhenAwayScoreExceedsMaximum_ShouldFailValidation(int score)
    {
        var cmd = ValidCommand() with { PredictedAwayScore = score };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PredictedAwayScore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(20)]
    public void UpdatePredictionValidator_WhenHomeScoreIsWithinRange_ShouldPassValidation(int score)
    {
        var cmd = ValidCommand() with { PredictedHomeScore = score };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.PredictedHomeScore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(20)]
    public void UpdatePredictionValidator_WhenAwayScoreIsWithinRange_ShouldPassValidation(int score)
    {
        var cmd = ValidCommand() with { PredictedAwayScore = score };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.PredictedAwayScore);
    }
}
