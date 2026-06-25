namespace TalentBridge.Applications.Domain.Entities;

public class ApplicationStatusHistory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ApplicationId { get; private set; }
    public string FromStatus { get; private set; } = string.Empty;
    public string ToStatus { get; private set; } = string.Empty;
    public Guid? ChangedByUserId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    private ApplicationStatusHistory() { }

    public static ApplicationStatusHistory Create(
        Guid applicationId,
        string fromStatus,
        string toStatus,
        Guid? changedByUserId = null,
        string? notes = null)
    {
        return new ApplicationStatusHistory
        {
            ApplicationId = applicationId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedByUserId = changedByUserId,
            Notes = notes,
            ChangedAtUtc = DateTime.UtcNow
        };
    }
}
