using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TalentBridge.Applications.Application.Interfaces;
using TalentBridge.Applications.Domain.Aggregates;
using TalentBridge.Shared.Outbox;

namespace TalentBridge.Applications.Infrastructure.Persistence;

public class ApplicationsDbContext : DbContext, IApplicationsDbContext
{
    public ApplicationsDbContext(DbContextOptions<ApplicationsDbContext> options) : base(options) { }

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    DatabaseFacade IApplicationsDbContext.Database => Database;

    Task<int> IApplicationsDbContext.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobApplication>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.CoverLetter).IsRequired().HasMaxLength(5000);
            e.Property(a => a.ResumeUrl).IsRequired();
            e.Property(a => a.Status).HasConversion<string>();
            e.HasIndex(a => a.JobId);
            e.HasIndex(a => a.CandidateId);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.ToTable("ApplicationsOutboxMessages");
        });
    }
}
