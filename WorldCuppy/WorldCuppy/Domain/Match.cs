namespace WorldCuppy.Domain;

public class Match
{
    public Guid Id { get; set; }
    public Guid HomeTeamId { get; set; }
    public required Team HomeTeam { get; set; }
    public Guid AwayTeamId { get; set; }
    public required Team AwayTeam { get; set; }
    public DateTimeOffset KickoffUtc { get; set; }
    public DateOnly GameDay { get; set; }
    public KnockoutRound Round { get; set; }
    public required string Venue { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
}
