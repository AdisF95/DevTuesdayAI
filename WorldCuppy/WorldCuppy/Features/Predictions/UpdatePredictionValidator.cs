using FluentValidation;

namespace WorldCuppy.Features.Predictions;

/// <summary>Validates <see cref="UpdatePredictionCommand" /> input.</summary>
public class UpdatePredictionValidator : AbstractValidator<UpdatePredictionCommand>
{
    /// <summary>Defines the validation rules.</summary>
    public UpdatePredictionValidator()
    {
        RuleFor(x => x.PredictionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PredictedHomeScore).GreaterThanOrEqualTo(0).LessThanOrEqualTo(20);
        RuleFor(x => x.PredictedAwayScore).GreaterThanOrEqualTo(0).LessThanOrEqualTo(20);
    }
}
