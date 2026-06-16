using FluentValidation;
using MediatR;
using WorldCuppy.Infrastructure.Behaviours;

namespace WorldCuppy.Infrastructure.Extensions;

/// <summary>Extension methods for registering MediatR and the validation pipeline.</summary>
public static class ApplicationExtensions
{
    /// <summary>Registers MediatR handlers, the validation behaviour, and all FluentValidation validators.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

        return services;
    }
}
