using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TalentBridge.Applications.Infrastructure.Persistence;

public class ApplicationsDbContextFactory : IDesignTimeDbContextFactory<ApplicationsDbContext>
{
    public ApplicationsDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__TalentBridgeDb")
            ?? "Server=tcp:tbsqlameydev.database.windows.net,1433;Initial Catalog=talentbridge-db;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        var options = new DbContextOptionsBuilder<ApplicationsDbContext>()
            .UseSqlServer(connStr)
            .Options;
        return new ApplicationsDbContext(options);
    }
}
