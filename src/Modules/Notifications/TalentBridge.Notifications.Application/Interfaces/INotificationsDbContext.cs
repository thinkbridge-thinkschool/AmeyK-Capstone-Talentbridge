using Microsoft.EntityFrameworkCore;
using TalentBridge.Notifications.Domain.Entities;

namespace TalentBridge.Notifications.Application.Interfaces;

public interface INotificationsDbContext
{
    DbSet<Notification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
