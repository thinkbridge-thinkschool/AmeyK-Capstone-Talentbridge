using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentBridge.Applications.Application.Interfaces;
using TalentBridge.Applications.Domain.Repositories;
using TalentBridge.Applications.Infrastructure.Persistence;
using TalentBridge.Applications.Infrastructure.Storage;

namespace TalentBridge.Applications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TalentBridgeDb")));

        services.AddScoped<IApplicationsDbContext>(sp => sp.GetRequiredService<ApplicationsDbContext>());
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IResumeStorageService, AzureResumeStorageService>();

        return services;
    }
}
