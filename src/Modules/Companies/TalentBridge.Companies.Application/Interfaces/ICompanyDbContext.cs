using Microsoft.EntityFrameworkCore;
using TalentBridge.Companies.Domain.Entities;

namespace TalentBridge.Companies.Application.Interfaces;

public interface ICompanyDbContext
{
    DbSet<Company> Companies { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
