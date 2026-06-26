using Microsoft.EntityFrameworkCore;
using TalentBridge.Jobs.Infrastructure.Persistence;
using TalentBridge.Shared.Interfaces;

namespace TalentBridge.Jobs.Infrastructure.Services;

public class JobLookupService : IJobLookupService
{
    private readonly JobsDbContext _db;

    public JobLookupService(JobsDbContext db) => _db = db;

    public async Task<JobSummary?> GetByIdAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _db.Jobs
            .AsNoTracking()
            .Where(j => j.Id == jobId)
            .Select(j => new JobSummary(j.Id, j.Title, j.Description, j.PostedByHRId))
            .FirstOrDefaultAsync(ct);
        return job;
    }
}
