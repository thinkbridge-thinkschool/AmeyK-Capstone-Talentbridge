using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TalentBridge.Jobs.Infrastructure.Persistence;

public class JobsDbContextFactory : IDesignTimeDbContextFactory<JobsDbContext>
{
    public JobsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<JobsDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TalentBridgeJobs;Trusted_Connection=True;")
            .Options;
        return new JobsDbContext(options);
    }
}
