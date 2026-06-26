using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentBridge.Jobs.Domain.Repositories;
using TalentBridge.Jobs.Infrastructure.Persistence;
using TalentBridge.Jobs.Infrastructure.Services;
using TalentBridge.Shared.Interfaces;

namespace TalentBridge.Jobs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddJobsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<JobsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TalentBridgeDb")));

        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IJobLookupService, JobLookupService>();

        return services;
    }
}
