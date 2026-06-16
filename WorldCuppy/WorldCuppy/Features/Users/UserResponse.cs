namespace WorldCuppy.Features.Users;

/// <summary>Public representation of a registered user.</summary>
public record UserResponse(Guid Id, string Username, string Email);
