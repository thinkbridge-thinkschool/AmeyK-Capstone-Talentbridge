using TalentBridge.Notifications.Application.Interfaces;
using TalentBridge.Notifications.Domain.Entities;
using TalentBridge.Shared.Interfaces;

namespace TalentBridge.Notifications.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationsDbContext _dbContext;

    public NotificationService(INotificationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(Guid userId, string message, CancellationToken ct = default)
    {
        var notification = Notification.Create(userId, message);
        await _dbContext.Notifications.AddAsync(notification, ct);
        await _dbContext.SaveChangesAsync(ct);
    }
}
