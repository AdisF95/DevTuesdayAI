using WorldCuppy.Features.Leaderboard;
using WorldCuppy.Features.Matches;
using WorldCuppy.Features.Predictions;
using WorldCuppy.Features.Sync;
using WorldCuppy.Features.Teams;
using WorldCuppy.Features.Users;

namespace WorldCuppy.Infrastructure.Extensions;

/// <summary>Extension methods for registering all API endpoints.</summary>
public static class EndpointExtensions
{
    /// <summary>Registers all feature endpoints onto <paramref name="app" />.</summary>
    public static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app)
    {
        MatchesEndpoints.MapEndpoints(app);
        LeaderboardEndpoints.MapEndpoints(app);
        PredictionsEndpoints.MapEndpoints(app);
        TeamsEndpoints.MapEndpoints(app);
        SyncEndpoints.MapEndpoints(app);
        UsersEndpoints.MapEndpoints(app);
        return app;
    }
}
