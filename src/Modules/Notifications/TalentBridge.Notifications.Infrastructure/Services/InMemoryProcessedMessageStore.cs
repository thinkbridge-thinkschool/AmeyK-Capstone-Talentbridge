using System.Collections.Concurrent;
using TalentBridge.Notifications.Infrastructure.Interfaces;

namespace TalentBridge.Notifications.Infrastructure.Services;

public class InMemoryProcessedMessageStore : IProcessedMessageStore
{
    private readonly ConcurrentDictionary<string, DateTime> _processed = new();

    public Task<bool> IsProcessedAsync(string messageId) =>
        Task.FromResult(_processed.ContainsKey(messageId));

    public Task MarkProcessedAsync(string messageId)
    {
        _processed[messageId] = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}
