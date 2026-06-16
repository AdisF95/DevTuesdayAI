using WorldCuppy.Features.Matches;

namespace WorldCuppy.Infrastructure.Extensions;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app)
    {
        MatchesEndpoints.MapEndpoints(app);
        return app;
    }
}
