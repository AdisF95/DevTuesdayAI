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

    /// <summary>Half-time home score; null until half-time is reached.</summary>
    public int? HalfTimeHomeScore { get; set; }

    /// <summary>Half-time away score; null until half-time is reached.</summary>
    public int? HalfTimeAwayScore { get; set; }

    /// <summary>Extra-time home score; null unless the match went to extra time.</summary>
    public int? ExtraTimeHomeScore { get; set; }

    /// <summary>Extra-time away score; null unless the match went to extra time.</summary>
    public int? ExtraTimeAwayScore { get; set; }

    /// <summary>Penalty-shootout home score; null unless the match was decided by penalties.</summary>
    public int? PenaltyHomeScore { get; set; }

    /// <summary>Penalty-shootout away score; null unless the match was decided by penalties.</summary>
    public int? PenaltyAwayScore { get; set; }

    /// <summary>How the match was concluded: REGULAR, EXTRA_TIME, or PENALTY_SHOOTOUT.</summary>
    public string? MatchDuration { get; set; }

    /// <summary>
    /// True once the match detail endpoint has been successfully called for this match.
    /// Prevents re-fetching matches that have 0 goals or 0 bookings from appearing
    /// as un-synced on every cycle.
    /// </summary>
    public bool EventsSynced { get; set; }

    /// <summary>Goals scored in this match, ordered by minute.</summary>
    public List<GoalEvent> GoalEvents { get; set; } = [];

    /// <summary>Bookings (yellow/red cards) issued during this match.</summary>
    public List<BookingEvent> BookingEvents { get; set; } = [];
}
