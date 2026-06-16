using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Predictions;

/// <summary>Query that retrieves all predictions submitted by a specific user.</summary>
public record GetPredictionsByUserQuery(Guid UserId) : IRequest<List<PredictionResponse>>;

/// <summary>Handles <see cref="GetPredictionsByUserQuery" />.</summary>
public class GetPredictionsByUserHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetPredictionsByUserQuery, List<PredictionResponse>>
{
    /// <summary>Executes the query against the database.</summary>
    public Task<List<PredictionResponse>> Handle(GetPredictionsByUserQuery request, CancellationToken cancellationToken) =>
        db.Predictions
            .Where(p => p.UserId == request.UserId)
            .OrderBy(p => p.MatchId)
            .Select(p => new PredictionResponse(
                p.Id,
                p.UserId,
                p.MatchId,
                p.PredictedHomeScore,
                p.PredictedAwayScore))
            .ToListAsync(cancellationToken);
}
