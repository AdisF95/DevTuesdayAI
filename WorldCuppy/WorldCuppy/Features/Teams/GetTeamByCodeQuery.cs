using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Teams;

/// <summary>Query that retrieves a single team by its 3-letter code.</summary>
public record GetTeamByCodeQuery(string Code) : IRequest<TeamResponse?>;

/// <summary>Handles <see cref="GetTeamByCodeQuery" />.</summary>
public class GetTeamByCodeHandler(WorldCuppyDbContext db)
    : IRequestHandler<GetTeamByCodeQuery, TeamResponse?>
{
    /// <summary>Executes the query against the database.</summary>
    public Task<TeamResponse?> Handle(GetTeamByCodeQuery request, CancellationToken cancellationToken) =>
        db.Teams
            .Where(t => t.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase))
            .Select(t => new TeamResponse(t.Id, t.Name, t.Code))
            .FirstOrDefaultAsync(cancellationToken);
}
