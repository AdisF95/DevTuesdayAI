using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WorldCuppy.Features.Leaderboard;

/// <summary>Registers all Leaderboard API routes.</summary>
public static class LeaderboardEndpoints
{
    /// <summary>Maps Leaderboard endpoints onto <paramref name="app" />.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/leaderboard").WithTags("Leaderboard");

        group.MapGet("/users", async Task<Ok<List<UserLeaderboardEntryResponse>>> (ISender sender) =>
        {
            var leaderboard = await sender.Send(new GetUserLeaderboardQuery());
            return TypedResults.Ok(leaderboard);
        })
        .WithName("GetUserLeaderboard")
        .WithSummary("Get the ranked prediction leaderboard of all users by total prediction points")
        .AllowAnonymous();
    }
}
