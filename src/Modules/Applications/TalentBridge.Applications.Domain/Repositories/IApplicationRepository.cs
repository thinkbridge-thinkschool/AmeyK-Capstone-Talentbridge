using TalentBridge.Applications.Domain.Aggregates;

namespace TalentBridge.Applications.Domain.Repositories;

public interface IApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<JobApplication>> GetByJobAsync(Guid jobId, CancellationToken ct);
    Task<List<JobApplication>> GetByCandidateAsync(Guid candidateId, CancellationToken ct);
    Task AddAsync(JobApplication application, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
