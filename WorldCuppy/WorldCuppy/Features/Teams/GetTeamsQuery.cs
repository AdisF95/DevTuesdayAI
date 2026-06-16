using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Teams;

/// <summary>Query that retrieves all teams.</summary>
public record GetTeamsQuery() : IRequest<List<TeamResponse>>;

/// <summary>Handles <see cref="GetTeamsQuery" />.</summary>
public class GetTeamsHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetTeamsQuery, List<TeamResponse>>
{
    /// <summary>Executes the query against the database.</summary>
    public Task<List<TeamResponse>> Handle(GetTeamsQuery request, CancellationToken cancellationToken) =>
        db.Teams
            .OrderBy(t => t.Name)
            .Select(t => new TeamResponse(t.Id, t.Name, t.Code, t.CrestUrl))
            .ToListAsync(cancellationToken);
}
