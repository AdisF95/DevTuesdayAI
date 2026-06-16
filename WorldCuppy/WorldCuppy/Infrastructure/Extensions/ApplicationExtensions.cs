using FluentValidation;
using MediatR;
using WorldCuppy.Infrastructure.Behaviours;

namespace WorldCuppy.Infrastructure.Extensions;

public static class ApplicationExtensions
{
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
