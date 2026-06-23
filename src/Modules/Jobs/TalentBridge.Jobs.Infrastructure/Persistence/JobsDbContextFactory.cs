using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TalentBridge.Jobs.Infrastructure.Persistence;

public class JobsDbContextFactory : IDesignTimeDbContextFactory<JobsDbContext>
{
    public JobsDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__TalentBridgeDb")
            ?? "Server=tcp:tbsqlameydev.database.windows.net,1433;Initial Catalog=talentbridge-db;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        var options = new DbContextOptionsBuilder<JobsDbContext>()
            .UseSqlServer(connStr)
            .Options;
        return new JobsDbContext(options);
    }
}
