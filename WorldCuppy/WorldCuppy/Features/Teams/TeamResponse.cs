namespace WorldCuppy.Features.Teams;

/// <summary>Response payload for Teams operations.</summary>
public record TeamResponse(
    Guid Id,
    string Name,
    string Code
);
