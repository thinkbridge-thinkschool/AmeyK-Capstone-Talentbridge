using Microsoft.EntityFrameworkCore;
using TalentBridge.Applications.Domain.Aggregates;
using TalentBridge.Applications.Domain.Repositories;

namespace TalentBridge.Applications.Infrastructure.Persistence;

public class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationsDbContext _context;

    public ApplicationRepository(ApplicationsDbContext context) => _context = context;

    public Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.JobApplications.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<List<JobApplication>> GetByJobAsync(Guid jobId, CancellationToken ct) =>
        _context.JobApplications.Where(a => a.JobId == jobId).ToListAsync(ct);

    public Task<List<JobApplication>> GetByCandidateAsync(Guid candidateId, CancellationToken ct) =>
        _context.JobApplications.Where(a => a.CandidateId == candidateId).ToListAsync(ct);

    public async Task AddAsync(JobApplication application, CancellationToken ct) =>
        await _context.JobApplications.AddAsync(application, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        _context.SaveChangesAsync(ct);
}
