using Microsoft.EntityFrameworkCore;
using TalentBridge.Jobs.Domain.Aggregates;
using TalentBridge.Shared.Outbox;

namespace TalentBridge.Jobs.Infrastructure.Persistence;

public class JobsDbContext : DbContext
{
    public JobsDbContext(DbContextOptions<JobsDbContext> options) : base(options) { }

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(e =>
        {
            e.HasKey(j => j.Id);
            e.Property(j => j.Title).IsRequired().HasMaxLength(200);
            e.Property(j => j.Description).IsRequired().HasMaxLength(5000);
            e.Property(j => j.SalaryMin).HasPrecision(18, 2);
            e.Property(j => j.SalaryMax).HasPrecision(18, 2);
            e.Property(j => j.Status).HasConversion<string>();
            e.HasIndex(j => j.Status);
            e.HasIndex(j => j.CompanyId);
            e.Ignore(j => j.DomainEvents);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.HasKey(o => o.Id);
            e.ToTable("JobsOutboxMessages");
        });
    }
}
