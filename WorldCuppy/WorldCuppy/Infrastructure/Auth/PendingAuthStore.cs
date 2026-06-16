using System.Collections.Concurrent;
using System.Security.Claims;

namespace WorldCuppy.Infrastructure.Auth;

/// <summary>
/// Singleton store for short-lived one-time auth tokens used to bridge the gap between
/// the Blazor SignalR circuit (where HttpContext is unavailable) and the cookie sign-in
/// endpoint (a normal HTTP request where HttpContext.SignInAsync works).
/// </summary>
public sealed class PendingAuthStore
{
    private readonly ConcurrentDictionary<Guid, (ClaimsPrincipal Principal, DateTime ExpiresAt)> _pending = new();

    /// <summary>Stores a <see cref="ClaimsPrincipal" /> and returns a single-use token valid for 5 minutes.</summary>
    public Guid Store(ClaimsPrincipal principal)
    {
        var token = Guid.NewGuid();
        _pending[token] = (principal, DateTime.UtcNow.AddMinutes(5));
        return token;
    }

    /// <summary>
    /// Removes and returns the <see cref="ClaimsPrincipal" /> for <paramref name="token" />,
    /// or <see langword="null" /> if the token is unknown or expired.
    /// </summary>
    public ClaimsPrincipal? Consume(Guid token)
    {
        if (_pending.TryRemove(token, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            return entry.Principal;
        }

        return null;
    }
}
