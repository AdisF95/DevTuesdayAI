namespace WorldCuppy.Domain;

/// <summary>A national team participating in the 2026 FIFA World Cup.</summary>
public class Team
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>football-data.org team id — used to correlate API responses with DB rows.</summary>
    public int ExternalId { get; set; }

    /// <summary>Full team name (e.g. "England").</summary>
    public required string Name { get; set; }

    /// <summary>Three-letter team code (e.g. "ENG").</summary>
    public required string Code { get; set; }

    /// <summary>URL of the team's crest image; null if not provided by the API.</summary>
    public string? CrestUrl { get; set; }
}
