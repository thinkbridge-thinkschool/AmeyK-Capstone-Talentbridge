using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentBridge.Companies.Application.Interfaces;
using TalentBridge.Companies.Infrastructure.Persistence;

namespace TalentBridge.Companies.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCompaniesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CompanyDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TalentBridgeDb")));

        services.AddScoped<ICompanyDbContext>(sp => sp.GetRequiredService<CompanyDbContext>());

        return services;
    }
}
