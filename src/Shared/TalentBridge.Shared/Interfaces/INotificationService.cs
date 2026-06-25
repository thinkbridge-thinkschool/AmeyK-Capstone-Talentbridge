namespace TalentBridge.Shared.Interfaces;

public interface INotificationService
{
    Task CreateAsync(Guid userId, string message, CancellationToken ct = default);
}
