using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Domain;
using WorldCuppy.Infrastructure.Auth;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Users;

/// <summary>Command that creates a new user account and returns a one-time auth token.</summary>
public record RegisterUserCommand(string Username, string Email, string Password) : IRequest<Guid>;

/// <summary>Handles <see cref="RegisterUserCommand" />.</summary>
public class RegisterUserHandler(WorldCuppyDbContext db, PendingAuthStore authStore)
    : IRequestHandler<RegisterUserCommand, Guid>
{
    /// <summary>Validates uniqueness, hashes the password, persists the user, and returns a pending-auth token.</summary>
    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var usernameTaken = await db.Users
            .AnyAsync(u => u.Username == request.Username, cancellationToken);
        if (usernameTaken)
        {
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");
        }

        var emailTaken = await db.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (emailTaken)
        {
            throw new InvalidOperationException("An account with that email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return authStore.Store(ClaimsPrincipalFactory.Create(user));
    }
}
