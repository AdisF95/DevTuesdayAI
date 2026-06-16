using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using WorldCuppy.Domain;

namespace WorldCuppy.Infrastructure.Auth;

/// <summary>Builds the <see cref="ClaimsPrincipal" /> stored in the auth cookie.</summary>
internal static class ClaimsPrincipalFactory
{
    /// <summary>Creates a cookie <see cref="ClaimsPrincipal" /> carrying the user's id and username.</summary>
    internal static ClaimsPrincipal Create(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
