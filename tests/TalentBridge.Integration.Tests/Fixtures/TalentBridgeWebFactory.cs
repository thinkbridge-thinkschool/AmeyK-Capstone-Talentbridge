using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using TalentBridge.Applications.Infrastructure.Persistence;
using TalentBridge.Companies.Infrastructure.Persistence;
using TalentBridge.Identity.Infrastructure.Persistence;
using TalentBridge.Jobs.Infrastructure.Persistence;
using TalentBridge.Notifications.Infrastructure.Consumers;
using TalentBridge.Notifications.Infrastructure.Persistence;
using TalentBridge.Notifications.Infrastructure.Relay;

namespace TalentBridge.Integration.Tests.Fixtures;

public class TalentBridgeWebFactory : WebApplicationFactory<Program>
{
    // Single secret used both by ConfigureAppConfiguration (so TokenService reads it)
    // and by PostConfigure<JwtBearerOptions> (so validation uses the same key).
    private const string TestJwtSecret = "test-integration-jwt-secret-key-32chars!!";

    // Shared root so all contexts in this factory instance use the same in-memory store.
    // Without this, EF Core's internal service provider caching can give each context its
    // own isolated root, causing data written in one request to be invisible to the next.
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // appsettings.Testing.json doesn't exist, so we inject required config inline.
        // The JWT Secret is normally in appsettings.Development.json which isn't loaded
        // in the Testing environment.
        builder.ConfigureAppConfiguration(config =>
        {
            // Provide JWT config so TokenService can sign tokens at runtime.
            // The same secret is also applied via PostConfigure<JwtBearerOptions> below
            // so token signing and validation always use identical keys regardless of
            // configuration load order.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = "talentbridge-api-amey",
                ["Jwt:Audience"] = "talentbridge-clients",
            });
        });

        builder.ConfigureServices(services =>
        {
            ReplaceWithInMemory<IdentityDbContext>(services, _dbRoot);
            ReplaceWithInMemory<JobsDbContext>(services, _dbRoot);
            ReplaceWithInMemory<ApplicationsDbContext>(services, _dbRoot);
            ReplaceWithInMemory<CompanyDbContext>(services, _dbRoot);
            ReplaceWithInMemory<NotificationsDbContext>(services, _dbRoot);
            ReplaceWithInMemory<RelayDbContext>(services, _dbRoot);

            // Remove Azure Service Bus background services — they require real Azure credentials
            var sbServices = services
                .Where(d => d.ImplementationType == typeof(OutboxRelayService) ||
                            d.ImplementationType == typeof(TalentBridgeEventConsumer))
                .ToList();
            foreach (var s in sbServices)
                services.Remove(s);

            // Disable rate limiting so test suites that make many auth requests don't hit 429.
            // Remove all existing RateLimiterOptions configurations (added by AddRateLimiter in
            // Program.cs) then replace with an unlimited no-op limiter.
            var configureType = typeof(Microsoft.Extensions.Options.IConfigureOptions<RateLimiterOptions>);
            var postConfigureType = typeof(Microsoft.Extensions.Options.IPostConfigureOptions<RateLimiterOptions>);
            var rateLimitDescs = services
                .Where(d => d.ServiceType == configureType || d.ServiceType == postConfigureType)
                .ToList();
            foreach (var d in rateLimitDescs) services.Remove(d);

            services.Configure<RateLimiterOptions>(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("test-global"));
                options.AddPolicy("auth", _ =>
                    RateLimitPartition.GetNoLimiter("test-auth"));
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            // Override the JWT validation signing key so it always matches what TokenService
            // uses to sign.  The DI module captures the key at registration time, which may
            // be before ConfigureAppConfiguration runs in some host configurations.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
            });
        });
    }

    private static void ReplaceWithInMemory<TContext>(
        IServiceCollection services, InMemoryDatabaseRoot dbRoot)
        where TContext : DbContext
    {
        // EF Core 9/10 registers IDbContextOptionsConfiguration<T> alongside DbContextOptions<T>.
        // Both must be removed so the SQL Server configurator doesn't survive and get merged
        // with our InMemory configurator when the options are resolved.
        var optionsConfigType = typeof(IDbContextOptionsConfiguration<TContext>);

        var toRemove = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<TContext>) ||
                        d.ServiceType == typeof(TContext) ||
                        d.ServiceType == optionsConfigType)
            .ToList();
        foreach (var d in toRemove)
            services.Remove(d);

        services.AddDbContext<TContext>(o =>
            o.UseInMemoryDatabase("TalentBridgeTestDb", dbRoot)
             .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
    }
}
