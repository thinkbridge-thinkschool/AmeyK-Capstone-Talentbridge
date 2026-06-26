using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TalentBridge.Applications.Application.Interfaces;
using TalentBridge.Applications.Domain.Aggregates;
using TalentBridge.Applications.Domain.Entities;
using TalentBridge.Shared.Outbox;

namespace TalentBridge.Applications.Infrastructure.Persistence;

public class ApplicationsDbContext : DbContext, IApplicationsDbContext
{
    public ApplicationsDbContext(DbContextOptions<ApplicationsDbContext> options) : base(options) { }

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<ApplicationStatusHistory> StatusHistory => Set<ApplicationStatusHistory>();
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
            e.Property(a => a.MatchPercentage).HasColumnType("decimal(5,2)");
            e.HasIndex(a => a.JobId);
            e.HasIndex(a => a.CandidateId);
            e.Ignore(a => a.DomainEvents);
        });

        modelBuilder.Entity<ApplicationStatusHistory>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.FromStatus).IsRequired().HasMaxLength(50);
            e.Property(h => h.ToStatus).IsRequired().HasMaxLength(50);
            e.Property(h => h.Notes).HasMaxLength(1000);
            e.HasIndex(h => h.ApplicationId);
            e.ToTable("ApplicationStatusHistory");
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.ToTable("ApplicationsOutboxMessages");
        });
    }
}
