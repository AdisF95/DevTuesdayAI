namespace WorldCuppy.Domain;

/// <summary>A yellow or red card booking issued during a match.</summary>
public class BookingEvent
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the match this booking belongs to.</summary>
    public Guid MatchId { get; set; }

    /// <summary>Navigation property to the parent match; populated by EF Core via MatchId.</summary>
    public Match Match { get; set; } = null!;

    /// <summary>Minute the card was shown.</summary>
    public int Minute { get; set; }

    /// <summary>Name of the player who received the card.</summary>
    public string? PlayerName { get; set; }

    /// <summary>Card type: YELLOW_CARD, RED_CARD, or YELLOW_RED_CARD.</summary>
    public required string CardType { get; set; }

    /// <summary>True when the booked player plays for the home team; false for the away team.</summary>
    public bool IsHomeTeam { get; set; }
}
