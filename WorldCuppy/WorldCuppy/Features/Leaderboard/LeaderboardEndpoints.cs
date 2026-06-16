using MediatR;

namespace WorldCuppy.Features.Leaderboard;

public static class LeaderboardEndpoints
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/leaderboard").WithTags("Leaderboard");

        group.MapGet("/", async (ISender sender) =>
        {
            var leaderboard = await sender.Send(new GetLeaderboardQuery());
            return TypedResults.Ok(leaderboard);
        })
        .WithName("GetLeaderboard")
        .WithSummary("Get the ranked leaderboard of all teams with their total points");
    }
}
