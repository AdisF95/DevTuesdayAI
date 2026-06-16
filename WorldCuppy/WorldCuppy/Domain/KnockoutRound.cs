namespace WorldCuppy.Domain;

/// <summary>Knockout stage of the 2026 FIFA World Cup tournament.</summary>
public enum KnockoutRound
{
    /// <summary>Round of 32 — the first knockout stage (48-team format).</summary>
    RoundOf32,

    /// <summary>Round of 16.</summary>
    RoundOf16,

    /// <summary>Quarter-final.</summary>
    QuarterFinal,

    /// <summary>Semi-final.</summary>
    SemiFinal,

    /// <summary>The final match.</summary>
    Final
}
