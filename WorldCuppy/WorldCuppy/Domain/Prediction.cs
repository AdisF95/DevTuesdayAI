namespace WorldCuppy.Domain;

/// <summary>Represents a user's score prediction for a match.</summary>
public class Prediction
{
    /// <summary>Unique identifier for the prediction.</summary>
    public Guid Id { get; set; }

    /// <summary>Identifier of the user who submitted the prediction.</summary>
    public Guid UserId { get; set; }

    /// <summary>Identifier of the match being predicted.</summary>
    public Guid MatchId { get; set; }

    /// <summary>Navigation property to the match.</summary>
    public required Match Match { get; set; }

    /// <summary>Predicted score for the home team.</summary>
    public int PredictedHomeScore { get; set; }

    /// <summary>Predicted score for the away team.</summary>
    public int PredictedAwayScore { get; set; }

    /// <summary>UTC timestamp of when the prediction was submitted.</summary>
    public DateTimeOffset SubmittedAtUtc { get; set; }

    /// <summary>Points awarded for this prediction. Null until the match finishes.</summary>
    public int? Points { get; set; }

    /// <summary>UTC timestamp of when points were awarded. Null until the match finishes.</summary>
    public DateTimeOffset? PointsAwardedAtUtc { get; set; }
}
