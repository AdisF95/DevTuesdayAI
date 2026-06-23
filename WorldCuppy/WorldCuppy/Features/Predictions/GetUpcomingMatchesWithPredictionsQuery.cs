using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Domain;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Predictions;

/// <summary>Query that retrieves all scheduled matches left-joined with the user's predictions.</summary>
public record GetUpcomingMatchesWithPredictionsQuery(Guid UserId) : IRequest<List<UpcomingMatchPredictionResponse>>;

/// <summary>Handles <see cref="GetUpcomingMatchesWithPredictionsQuery" />.</summary>
public class GetUpcomingMatchesWithPredictionsHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetUpcomingMatchesWithPredictionsQuery, List<UpcomingMatchPredictionResponse>>
{
    /// <summary>Executes a left-join of scheduled matches with the user's predictions, ordered by kickoff ascending.</summary>
    public Task<List<UpcomingMatchPredictionResponse>> Handle(
        GetUpcomingMatchesWithPredictionsQuery request,
        CancellationToken cancellationToken) =>
        db.Matches
            .Where(m => m.Status == MatchStatus.Scheduled)
            .OrderBy(m => m.KickoffUtc)
            .Select(m => new UpcomingMatchPredictionResponse(
                m.Id,
                m.HomeTeam.Name,
                m.HomeTeam.Code,
                m.HomeTeam.CrestUrl,
                m.AwayTeam.Name,
                m.AwayTeam.Code,
                m.AwayTeam.CrestUrl,
                m.KickoffUtc,
                m.GameDay.DayNumber,
                db.Predictions
                    .Where(p => p.MatchId == m.Id && p.UserId == request.UserId)
                    .Select(p => (Guid?)p.Id)
                    .FirstOrDefault(),
                db.Predictions
                    .Where(p => p.MatchId == m.Id && p.UserId == request.UserId)
                    .Select(p => (int?)p.PredictedHomeScore)
                    .FirstOrDefault(),
                db.Predictions
                    .Where(p => p.MatchId == m.Id && p.UserId == request.UserId)
                    .Select(p => (int?)p.PredictedAwayScore)
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);
}
