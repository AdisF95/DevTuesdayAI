namespace WorldCuppy.Domain;

/// <summary>A team's current position and statistics in a World Cup group.</summary>
public class GroupStanding
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>FIFA group label, e.g. "GROUP_A".</summary>
    public required string Group { get; set; }

    /// <summary>Foreign key to the team.</summary>
    public Guid TeamId { get; set; }

    /// <summary>Navigation property to the team.</summary>
    public required Team Team { get; set; }

    /// <summary>Position within the group (1 = top).</summary>
    public int Position { get; set; }

    /// <summary>Number of matches played.</summary>
    public int PlayedGames { get; set; }

    /// <summary>Number of matches won.</summary>
    public int Won { get; set; }

    /// <summary>Number of matches drawn.</summary>
    public int Draw { get; set; }

    /// <summary>Number of matches lost.</summary>
    public int Lost { get; set; }

    /// <summary>Total goals scored.</summary>
    public int GoalsFor { get; set; }

    /// <summary>Total goals conceded.</summary>
    public int GoalsAgainst { get; set; }

    /// <summary>Goal difference (GoalsFor − GoalsAgainst).</summary>
    public int GoalDifference { get; set; }

    /// <summary>Total points accumulated.</summary>
    public int Points { get; set; }

    /// <summary>Last 5 results as a comma-separated string, e.g. "W,D,L,W,W"; null when no games played.</summary>
    public string? Form { get; set; }
}
