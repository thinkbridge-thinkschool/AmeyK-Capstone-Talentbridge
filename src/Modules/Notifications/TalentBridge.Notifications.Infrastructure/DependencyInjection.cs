using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentBridge.Notifications.Infrastructure.Consumers;
using TalentBridge.Notifications.Infrastructure.Interfaces;
using TalentBridge.Notifications.Infrastructure.Relay;
using TalentBridge.Notifications.Infrastructure.Services;

namespace TalentBridge.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<RelayDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TalentBridgeDb")));

        services.AddScoped<IRelayDbContext>(sp => sp.GetRequiredService<RelayDbContext>());
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddSingleton<IProcessedMessageStore, InMemoryProcessedMessageStore>();
        services.AddHostedService<TalentBridgeEventConsumer>();
        services.AddHostedService<OutboxRelayService>();

        return services;
    }
}
