using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TalentBridge.Applications.Domain.Aggregates;
using TalentBridge.Shared.Outbox;

namespace TalentBridge.Applications.Application.Interfaces;

public interface IApplicationsDbContext
{
    DbSet<JobApplication> JobApplications { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
