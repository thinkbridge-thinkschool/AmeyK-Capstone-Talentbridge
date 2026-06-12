using Microsoft.EntityFrameworkCore;
using TalentBridge.Identity.Domain.Entities;

namespace TalentBridge.Identity.Application.Interfaces;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
