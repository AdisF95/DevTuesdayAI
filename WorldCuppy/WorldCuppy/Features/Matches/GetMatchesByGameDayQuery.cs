using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Matches;

public record GetMatchesByGameDayQuery(DateOnly GameDay) : IRequest<List<MatchResponse>>;

public class GetMatchesByGameDayHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetMatchesByGameDayQuery, List<MatchResponse>>
{
    public Task<List<MatchResponse>> Handle(GetMatchesByGameDayQuery request, CancellationToken cancellationToken) =>
        db.Matches
            .Where(m => m.GameDay == request.GameDay)
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
