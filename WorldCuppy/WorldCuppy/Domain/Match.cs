namespace WorldCuppy.Domain;

/// <summary>A scheduled or completed fixture between two teams.</summary>
public class Match
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>football-data.org match id — used to correlate API responses with DB rows.</summary>
    public int ExternalId { get; set; }

    /// <summary>Foreign key to the home team.</summary>
    public Guid HomeTeamId { get; set; }

    /// <summary>Navigation property to the home team.</summary>
    public required Team HomeTeam { get; set; }

    /// <summary>Foreign key to the away team.</summary>
    public Guid AwayTeamId { get; set; }

    /// <summary>Navigation property to the away team.</summary>
    public required Team AwayTeam { get; set; }

    /// <summary>Scheduled kickoff time in UTC.</summary>
    public DateTimeOffset KickoffUtc { get; set; }

    /// <summary>Calendar date of the match (UTC), used for day-based grouping.</summary>
    public DateOnly GameDay { get; set; }

    /// <summary>Null for group-stage matches; set for all knockout rounds.</summary>
    public KnockoutRound? Round { get; set; }

    /// <summary>Current lifecycle status of the match.</summary>
    public MatchStatus Status { get; set; }

    /// <summary>FIFA group label (e.g. "GROUP_A"); null for knockout matches.</summary>
    public string? Group { get; set; }

    /// <summary>Name of the stadium hosting the match.</summary>
    public required string Venue { get; set; }

    /// <summary>Full-time home team score; null until the match is finished.</summary>
    public int? HomeScore { get; set; }

    /// <summary>Full-time away team score; null until the match is finished.</summary>
    public int? AwayScore { get; set; }
}
