using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Features.Sync;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Predictions;

/// <summary>
/// Handles <see cref="MatchFinishedEvent" /> by awarding points to all predictions
/// for the finished match that have not yet been scored.
/// </summary>
public class AwardPredictionPointsHandler(WorldCuppyDbContext db)
    : INotificationHandler<MatchFinishedEvent>
{
    /// <summary>
    /// Loads all unscored predictions for the finished match, calculates points via
    /// <see cref="PredictionPointsCalculator" />, persists the result, and stamps the award time.
    /// Predictions that already have <c>PointsAwardedAtUtc</c> set are skipped (idempotent).
    /// </summary>
    public async Task Handle(MatchFinishedEvent notification, CancellationToken cancellationToken)
    {
        var predictions = await db.Predictions
            .Where(p => p.MatchId == notification.MatchId && p.PointsAwardedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (predictions.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var prediction in predictions)
        {
            prediction.Points               = PredictionPointsCalculator.Calculate(
                prediction.PredictedHomeScore,
                prediction.PredictedAwayScore,
                notification.HomeScore,
                notification.AwayScore);
            prediction.PointsAwardedAtUtc   = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
