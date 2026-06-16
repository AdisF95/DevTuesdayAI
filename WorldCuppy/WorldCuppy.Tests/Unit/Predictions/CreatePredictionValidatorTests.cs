using Bogus;
using FluentValidation.TestHelper;
using WorldCuppy.Features.Predictions;

namespace WorldCuppy.Tests.Unit.Predictions;

/// <summary>Unit tests for <see cref="CreatePredictionValidator" />.</summary>
public class CreatePredictionValidatorTests
{
    private readonly CreatePredictionValidator _validator = new();
    private readonly Faker _faker = new();

    /// <summary>A valid command built with Bogus — all rule variations start from this baseline.</summary>
    private CreatePredictionCommand ValidCommand() => new(
        UserId: _faker.Random.Guid(),
        MatchId: _faker.Random.Guid(),
        PredictedHomeScore: _faker.Random.Int(0, 10),
        PredictedAwayScore: _faker.Random.Int(0, 10));

    [Fact]
    public void CreatePredictionValidator_WhenCommandIsValid_ShouldPassAllRules()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreatePredictionValidator_WhenUserIdIsEmpty_ShouldFailValidation()
    {
        var cmd = ValidCommand() with { UserId = Guid.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void CreatePredictionValidator_WhenMatchIdIsEmpty_ShouldFailValidation()
    {
        var cmd = ValidCommand() with { MatchId = Guid.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MatchId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CreatePredictionValidator_WhenHomeScoreIsNegative_ShouldFailValidation(int score)
    {
        var cmd = ValidCommand() with { PredictedHomeScore = score };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PredictedHomeScore);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void CreatePredictionValidator_WhenAwayScoreIsNegative_ShouldFailValidation(int score)
    {
        var cmd = ValidCommand() with { PredictedAwayScore = score };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PredictedAwayScore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void CreatePredictionValidator_WhenHomeScoreIsZeroOrPositive_ShouldPassValidation(int score)
    {
        var cmd = ValidCommand() with { PredictedHomeScore = score };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.PredictedHomeScore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void CreatePredictionValidator_WhenAwayScoreIsZeroOrPositive_ShouldPassValidation(int score)
    {
        var cmd = ValidCommand() with { PredictedAwayScore = score };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.PredictedAwayScore);
    }
}
