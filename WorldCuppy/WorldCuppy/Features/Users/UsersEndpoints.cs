using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WorldCuppy.Infrastructure.Auth;

namespace WorldCuppy.Features.Users;

/// <summary>Minimal API endpoints that handle the cookie sign-in and sign-out flows.</summary>
public static class UsersEndpoints
{
    /// <summary>Registers account-related endpoints on <paramref name="app" />.</summary>
    public static IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/account");

        // Consumes a one-time pending-auth token, sets the auth cookie, and redirects home.
        group.MapGet("/complete-auth/{token:guid}", async (Guid token, PendingAuthStore store, HttpContext ctx) =>
        {
            var principal = store.Consume(token);
            if (principal is null)
            {
                return Results.Redirect("/login?error=session-expired");
            }

            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return Results.Redirect("/");
        })
        .WithName("CompleteAuth")
        .WithSummary("Completes cookie sign-in from a Blazor circuit.");

        // Clears the auth cookie and redirects home.
        group.MapGet("/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        })
        .WithName("Logout")
        .WithSummary("Signs the current user out.");

        return app;
    }
}
