using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TalentBridge.Applications.Infrastructure.Persistence;

public class ApplicationsDbContextFactory : IDesignTimeDbContextFactory<ApplicationsDbContext>
{
    public ApplicationsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationsDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TalentBridgeApplications;Trusted_Connection=True;")
            .Options;
        return new ApplicationsDbContext(options);
    }
}
