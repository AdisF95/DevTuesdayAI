using Microsoft.AspNetCore.Authentication.Cookies;
using WorldCuppy.Infrastructure.Auth;

namespace WorldCuppy.Infrastructure.Extensions;

/// <summary>Extension methods for wiring up cookie authentication.</summary>
public static class AuthExtensions
{
    /// <summary>
    /// Registers cookie authentication, authorization, cascading auth state for Blazor,
    /// and the <see cref="PendingAuthStore" /> singleton used to bridge the Blazor circuit → HTTP sign-in flow.
    /// </summary>
    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddSingleton<PendingAuthStore>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
            });

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }
}
