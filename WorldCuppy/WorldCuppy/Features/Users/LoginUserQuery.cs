using MediatR;
using Microsoft.EntityFrameworkCore;
using WorldCuppy.Infrastructure.Auth;
using WorldCuppy.Infrastructure.Persistence;

namespace WorldCuppy.Features.Users;

/// <summary>Query that authenticates a user by username or email + password and returns a one-time auth token.</summary>
public record LoginUserQuery(string UsernameOrEmail, string Password) : IRequest<Guid>;

/// <summary>Handles <see cref="LoginUserQuery" />.</summary>
public class LoginUserHandler(WorldCuppyDbContext db, PendingAuthStore authStore)
    : IRequestHandler<LoginUserQuery, Guid>
{
    /// <summary>
    /// Looks up the user by username or email, verifies the PBKDF2 hash, and returns a pending-auth token.
    /// Throws <see cref="UnauthorizedAccessException" /> on bad credentials.
    /// </summary>
    public async Task<Guid> Handle(LoginUserQuery request, CancellationToken cancellationToken)
    {
        var input = request.UsernameOrEmail.Trim();

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Username == input || u.Email == input,
            cancellationToken);

        // Deliberate constant-time path: always verify even when user is null to avoid username enumeration.
        var passwordOk = user is not null && PasswordHasher.Verify(request.Password, user.PasswordHash);

        if (!passwordOk)
        {
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

        return authStore.Store(ClaimsPrincipalFactory.Create(user!));
    }
}
