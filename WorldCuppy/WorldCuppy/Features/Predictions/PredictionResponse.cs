namespace WorldCuppy.Features.Predictions;

/// <summary>Response payload for Predictions operations.</summary>
public record PredictionResponse(
    Guid Id,
    Guid UserId,
    Guid MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore
);
