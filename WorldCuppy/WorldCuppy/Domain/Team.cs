namespace WorldCuppy.Domain;

public class Team
{
    public Guid Id { get; set; }
    /// <summary>football-data.org team id — used to correlate API responses with DB rows.</summary>
    public int ExternalId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? CrestUrl { get; set; }
}
