using Microsoft.EntityFrameworkCore;
using TalentBridge.Companies.Application.Interfaces;
using TalentBridge.Companies.Domain.Entities;

namespace TalentBridge.Companies.Infrastructure.Persistence;

public class CompanyDbContext : DbContext, ICompanyDbContext
{
    public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();

    Task<int> ICompanyDbContext.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.Property(c => c.Description).HasMaxLength(2000);
            e.Property(c => c.Website).HasMaxLength(500);
            e.HasIndex(c => c.OwnerId);
            e.Ignore(c => c.DomainEvents);
        });
    }
}
