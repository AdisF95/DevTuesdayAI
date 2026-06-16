using FluentValidation;

namespace WorldCuppy.Features.Predictions;

/// <summary>Validates <see cref="CreatePredictionCommand" /> input.</summary>
public class CreatePredictionValidator : AbstractValidator<CreatePredictionCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public CreatePredictionValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.MatchId).NotEmpty();
        RuleFor(x => x.PredictedHomeScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PredictedAwayScore).GreaterThanOrEqualTo(0);
    }
}
