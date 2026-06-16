using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Matches;

/// <summary>Query that retrieves a single match by its identifier.</summary>
public record GetMatchByIdQuery(Guid MatchId) : IRequest<MatchResponse?>;

/// <summary>Handles <see cref="GetMatchByIdQuery" />.</summary>
public class GetMatchByIdHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetMatchByIdQuery, MatchResponse?>
{
    /// <summary>Executes the query against the database.</summary>
    public Task<MatchResponse?> Handle(GetMatchByIdQuery request, CancellationToken cancellationToken) =>
        db.Matches
            .Where(m => m.Id == request.MatchId)
            .Select(m => new MatchResponse(
                m.Id,
                m.HomeTeam.Name,
                m.HomeTeam.Code,
                m.HomeTeam.CrestUrl,
                m.AwayTeam.Name,
                m.AwayTeam.Code,
                m.AwayTeam.CrestUrl,
                m.KickoffUtc,
                m.GameDay,
                m.Round.ToString(),
                m.Status.ToString(),
                m.Group,
                m.Venue,
                m.HomeScore,
                m.AwayScore))
            .FirstOrDefaultAsync(cancellationToken);
}
