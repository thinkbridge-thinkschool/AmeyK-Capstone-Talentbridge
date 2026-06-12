using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace TalentBridge.Applications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
