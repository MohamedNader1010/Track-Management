using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using TrackManagement.Application.Common.Behaviors;

namespace TrackManagement.Application;

public static class DependencyInjection
{
    // C# 14 extension members.
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(assembly);

                // Order matters: logging wraps validation, so rejected requests are logged too.
                config.AddOpenBehavior(typeof(LoggingBehavior<,>));
                config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

            return services;
        }
    }
}
