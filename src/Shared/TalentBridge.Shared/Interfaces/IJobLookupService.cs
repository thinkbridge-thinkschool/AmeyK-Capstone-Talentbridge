namespace TalentBridge.Shared.Interfaces;

public record JobSummary(Guid Id, string Title, string Description, Guid? PostedByHRId = null);

public interface IJobLookupService
{
    Task<JobSummary?> GetByIdAsync(Guid jobId, CancellationToken ct = default);
}
