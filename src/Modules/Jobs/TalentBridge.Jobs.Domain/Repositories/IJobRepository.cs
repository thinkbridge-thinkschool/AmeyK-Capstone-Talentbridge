using TalentBridge.Jobs.Domain.Aggregates;

namespace TalentBridge.Jobs.Domain.Repositories;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Job>> GetActiveJobsAsync(int page, int size, CancellationToken ct);
    Task<List<Job>> SearchAsync(string keyword, string? location, CancellationToken ct);
    Task<List<Job>> GetByCompanyAsync(Guid companyId, CancellationToken ct);
    Task AddAsync(Job job, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
