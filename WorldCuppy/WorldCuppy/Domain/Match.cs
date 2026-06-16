namespace WorldCuppy.Domain;

public class Match
{
    public Guid Id { get; set; }
    /// <summary>football-data.org match id — used to correlate API responses with DB rows.</summary>
    public int ExternalId { get; set; }
    public Guid HomeTeamId { get; set; }
    public required Team HomeTeam { get; set; }
    public Guid AwayTeamId { get; set; }
    public required Team AwayTeam { get; set; }
    public DateTimeOffset KickoffUtc { get; set; }
    public DateOnly GameDay { get; set; }
    /// <summary>Null for group-stage matches; set for all knockout rounds.</summary>
    public KnockoutRound? Round { get; set; }
    public MatchStatus Status { get; set; }
    public string? Group { get; set; }
    public required string Venue { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
}
