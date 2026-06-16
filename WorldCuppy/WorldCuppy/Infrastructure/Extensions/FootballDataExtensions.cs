using Microsoft.Extensions.Options;
using WorldCuppy.Infrastructure.FootballData;

namespace WorldCuppy.Infrastructure.Extensions;

/// <summary>Service registration for the football-data.org HTTP client.</summary>
public static class FootballDataExtensions
{
    /// <summary>Registers <see cref="FootballDataClient" /> and its configuration.</summary>
    public static IServiceCollection AddFootballData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FootballDataOptions>(configuration.GetSection("FootballData"));

        services.AddHttpClient<FootballDataClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<FootballDataOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("X-Auth-Token", options.ApiKey);
        });

        return services;
    }
}

/// <summary>Configuration options for the football-data.org integration.</summary>
public class FootballDataOptions
{
    /// <summary>API key obtained from https://www.football-data.org/client/register.</summary>
    public required string ApiKey { get; set; }

    /// <summary>Base URL for the API — defaults to v4.</summary>
    public string BaseUrl { get; set; } = "https://api.football-data.org/v4/";
}
