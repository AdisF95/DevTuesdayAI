using MediatR;

namespace WorldCuppy.Features.Sync;

/// <summary>Published when a match transitions to the Finished status with a confirmed final score.</summary>
/// <param name="MatchId">Id of the match that just finished.</param>
/// <param name="HomeScore">Final home team score.</param>
/// <param name="AwayScore">Final away team score.</param>
public record MatchFinishedEvent(
    Guid MatchId,
    int HomeScore,
    int AwayScore
) : INotification;
