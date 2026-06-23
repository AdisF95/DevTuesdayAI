namespace WorldCuppy.Features.Predictions;

/// <summary>Response payload for a scheduled match combined with the user's prediction (if any).</summary>
public record UpcomingMatchPredictionResponse(
    Guid     MatchId,
    string   HomeTeamName,
    string   HomeTeamCode,
    string?  HomeTeamCrestUrl,
    string   AwayTeamName,
    string   AwayTeamCode,
    string?  AwayTeamCrestUrl,
    DateTimeOffset KickoffUtc,
    int      GameDay,
    /// <summary>Null when the user has no prediction for this match yet.</summary>
    Guid?    PredictionId,
    int?     PredictedHomeScore,
    int?     PredictedAwayScore
);
