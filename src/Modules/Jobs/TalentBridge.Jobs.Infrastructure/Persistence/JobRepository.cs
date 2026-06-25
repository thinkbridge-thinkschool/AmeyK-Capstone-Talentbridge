using Microsoft.EntityFrameworkCore;
using TalentBridge.Jobs.Domain.Aggregates;
using TalentBridge.Jobs.Domain.Enums;
using TalentBridge.Jobs.Domain.Repositories;

namespace TalentBridge.Jobs.Infrastructure.Persistence;

public class JobRepository : IJobRepository
{
    private readonly JobsDbContext _context;

    public JobRepository(JobsDbContext context) => _context = context;

    public Task<Job?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<List<Job>> GetActiveJobsAsync(int page, int size, CancellationToken ct) =>
        _context.Jobs
            .Where(j => j.Status == JobStatus.Active)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

    public Task<List<Job>> SearchAsync(string keyword, string? location, decimal? salaryMin, decimal? salaryMax, CancellationToken ct) =>
        _context.Jobs
            .Where(j => j.Status == JobStatus.Active &&
                (string.IsNullOrEmpty(keyword) || j.Title.Contains(keyword) || j.Description.Contains(keyword)) &&
                (string.IsNullOrEmpty(location) || j.Location.Contains(location)) &&
                (salaryMin == null || j.SalaryMax >= salaryMin) &&
                (salaryMax == null || j.SalaryMin <= salaryMax))
            .OrderByDescending(j => j.PublishedAtUtc)
            .ToListAsync(ct);

    public Task<List<Job>> GetByCompanyAsync(Guid companyId, CancellationToken ct) =>
        _context.Jobs.Where(j => j.CompanyId == companyId).ToListAsync(ct);

    public Task<List<Job>> GetByHRIdAsync(Guid hrId, CancellationToken ct) =>
        _context.Jobs.Where(j => j.PostedByHRId == hrId).OrderByDescending(j => j.CreatedAtUtc).ToListAsync(ct);

    public Task<List<Job>> GetAllAsync(CancellationToken ct) =>
        _context.Jobs.OrderByDescending(j => j.CreatedAtUtc).ToListAsync(ct);

    public async Task AddAsync(Job job, CancellationToken ct) =>
        await _context.Jobs.AddAsync(job, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var job = await _context.Jobs.FindAsync([id], ct);
        if (job is not null)
            _context.Jobs.Remove(job);
    }

    public Task SaveChangesAsync(CancellationToken ct) =>
        _context.SaveChangesAsync(ct);
}
