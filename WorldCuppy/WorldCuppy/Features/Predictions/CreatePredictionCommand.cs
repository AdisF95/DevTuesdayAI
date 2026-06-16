using MediatR;
using WorldCuppy.Domain;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Predictions;

/// <summary>Command that creates a new match prediction for a user.</summary>
public record CreatePredictionCommand(
    Guid UserId,
    Guid MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore
) : IRequest<PredictionResponse>;

/// <summary>Handles <see cref="CreatePredictionCommand" />.</summary>
public class CreatePredictionHandler(WorldCuppyDbContext db)
    : IRequestHandler<CreatePredictionCommand, PredictionResponse>
{
    /// <summary>Persists the new prediction and returns the created resource.</summary>
    public async Task<PredictionResponse> Handle(CreatePredictionCommand request, CancellationToken cancellationToken)
    {
        var match = await db.Matches.FindAsync([request.MatchId], cancellationToken)
            ?? throw new InvalidOperationException($"Match {request.MatchId} not found.");

        var entity = new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            MatchId = request.MatchId,
            Match = match,
            PredictedHomeScore = request.PredictedHomeScore,
            PredictedAwayScore = request.PredictedAwayScore,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
        };

        db.Predictions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new PredictionResponse(
            entity.Id,
            entity.UserId,
            entity.MatchId,
            entity.PredictedHomeScore,
            entity.PredictedAwayScore
        );
    }
}
