using MediatR;
using WorldCuppy.Domain;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Predictions;

/// <summary>Command that updates the predicted scores on an existing prediction.</summary>
public record UpdatePredictionCommand(
    Guid PredictionId,
    Guid UserId,
    int PredictedHomeScore,
    int PredictedAwayScore
) : IRequest<PredictionResponse>;

/// <summary>Handles <see cref="UpdatePredictionCommand" />.</summary>
public class UpdatePredictionHandler(WorldCuppyDbContext db)
    : IRequestHandler<UpdatePredictionCommand, PredictionResponse>
{
    /// <summary>
    /// Loads the prediction, verifies ownership and match status, persists the new scores,
    /// and returns the updated resource.
    /// </summary>
    public async Task<PredictionResponse> Handle(UpdatePredictionCommand request, CancellationToken cancellationToken)
    {
        var prediction = await db.Predictions.FindAsync([request.PredictionId], cancellationToken);

        if (prediction is null || prediction.UserId != request.UserId)
        {
            throw new KeyNotFoundException($"Prediction {request.PredictionId} not found.");
        }

        var match = await db.Matches.FindAsync([prediction.MatchId], cancellationToken)
            ?? throw new InvalidOperationException($"Match {prediction.MatchId} not found.");

        if (match.Status != MatchStatus.Scheduled)
        {
            throw new InvalidOperationException("Match is no longer open for predictions.");
        }

        prediction.PredictedHomeScore = request.PredictedHomeScore;
        prediction.PredictedAwayScore = request.PredictedAwayScore;

        await db.SaveChangesAsync(cancellationToken);

        return new PredictionResponse(
            prediction.Id,
            prediction.UserId,
            prediction.MatchId,
            prediction.PredictedHomeScore,
            prediction.PredictedAwayScore
        );
    }
}
