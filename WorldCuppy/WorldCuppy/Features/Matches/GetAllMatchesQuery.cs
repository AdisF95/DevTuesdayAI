using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Matches;

/// <summary>Query that retrieves every match in the tournament, ordered by kickoff time.</summary>
public record GetAllMatchesQuery : IRequest<List<MatchResponse>>;

/// <summary>Handles <see cref="GetAllMatchesQuery" />.</summary>
public class GetAllMatchesHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetAllMatchesQuery, List<MatchResponse>>
{
    /// <summary>Executes the query against the database.</summary>
    public Task<List<MatchResponse>> Handle(GetAllMatchesQuery request, CancellationToken cancellationToken) =>
        db.Matches
            .OrderBy(m => m.KickoffUtc)
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
            .ToListAsync(cancellationToken);
}
