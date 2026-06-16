namespace WorldCuppy.Domain;

/// <summary>A goal scored during a match, including scorer and timing details.</summary>
public class GoalEvent
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the match this goal belongs to.</summary>
    public Guid MatchId { get; set; }

    /// <summary>Navigation property to the parent match; populated by EF Core via MatchId.</summary>
    public Match Match { get; set; } = null!;

    /// <summary>Minute of the goal (raw minute from the API, e.g. 93 for 90+3').</summary>
    public int Minute { get; set; }

    /// <summary>Name of the goal scorer; may be null for own goals where scorer is unattributed.</summary>
    public string? ScorerName { get; set; }

    /// <summary>Name of the player who provided the assist; null when no assist is recorded.</summary>
    public string? AssistName { get; set; }

    /// <summary>Goal type: REGULAR, OWN_GOAL, or PENALTY.</summary>
    public required string Type { get; set; }

    /// <summary>Name of the team that the goal is attributed to.</summary>
    public required string TeamName { get; set; }

    /// <summary>True when the goal was scored by the home team; false for the away team.</summary>
    public bool IsHomeTeam { get; set; }
}
