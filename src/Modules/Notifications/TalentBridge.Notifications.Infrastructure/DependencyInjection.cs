using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentBridge.Notifications.Application.Interfaces;
using TalentBridge.Notifications.Infrastructure.Consumers;
using TalentBridge.Notifications.Infrastructure.Interfaces;
using TalentBridge.Notifications.Infrastructure.Persistence;
using TalentBridge.Notifications.Infrastructure.Relay;
using TalentBridge.Notifications.Infrastructure.Services;
using TalentBridge.Shared.Interfaces;

namespace TalentBridge.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<RelayDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TalentBridgeDb")));

        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TalentBridgeDb")));

        services.AddScoped<IRelayDbContext>(sp => sp.GetRequiredService<RelayDbContext>());
        services.AddScoped<INotificationsDbContext>(sp => sp.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddSingleton<IProcessedMessageStore, InMemoryProcessedMessageStore>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddHostedService<TalentBridgeEventConsumer>();
        services.AddHostedService<OutboxRelayService>();

        return services;
    }
}
