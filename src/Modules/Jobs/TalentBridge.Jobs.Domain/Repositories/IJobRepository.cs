using TalentBridge.Jobs.Domain.Aggregates;

namespace TalentBridge.Jobs.Domain.Repositories;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Job>> GetActiveJobsAsync(int page, int size, CancellationToken ct);
    Task<List<Job>> SearchAsync(string keyword, string? location, decimal? salaryMin, decimal? salaryMax, CancellationToken ct);
    Task<List<Job>> GetByCompanyAsync(Guid companyId, CancellationToken ct);
    Task<List<Job>> GetByHRIdAsync(Guid hrId, CancellationToken ct);
    Task<List<Job>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Job job, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
