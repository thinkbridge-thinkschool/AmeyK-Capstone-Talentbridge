namespace TalentBridge.Notifications.Domain.Entities;

public class NotificationRecord
{
    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public bool IsSent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public int AttemptCount { get; private set; }

    private NotificationRecord() { }

    public static NotificationRecord Create(Guid recipientId, string subject, string body, NotificationType type) =>
        new()
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientId,
            Subject = subject,
            Body = body,
            Type = type,
            IsSent = false,
            CreatedAtUtc = DateTime.UtcNow,
            AttemptCount = 0
        };
}

public enum NotificationType { Email = 0, Push = 1 }
