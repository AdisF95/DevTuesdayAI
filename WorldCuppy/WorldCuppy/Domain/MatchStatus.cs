namespace WorldCuppy.Domain;

/// <summary>The current lifecycle status of a match as reported by the data provider.</summary>
public enum MatchStatus
{
    Scheduled,
    Live,
    Finished,
    Postponed,
    Cancelled
}
