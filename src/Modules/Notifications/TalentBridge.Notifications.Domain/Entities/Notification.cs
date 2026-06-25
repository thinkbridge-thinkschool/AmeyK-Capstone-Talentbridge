namespace TalentBridge.Notifications.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public static Notification Create(Guid userId, string message) => new()
    {
        UserId = userId,
        Message = message,
        IsRead = false,
        CreatedAtUtc = DateTime.UtcNow
    };
}
