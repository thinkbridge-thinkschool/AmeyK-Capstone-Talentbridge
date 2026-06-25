using Microsoft.EntityFrameworkCore;
using TalentBridge.Applications.Domain.Aggregates;
using TalentBridge.Applications.Infrastructure.Persistence;
using TalentBridge.Companies.Domain.Entities;
using TalentBridge.Companies.Infrastructure.Persistence;
using TalentBridge.Identity.Domain.Entities;
using TalentBridge.Identity.Domain.Enums;
using TalentBridge.Identity.Infrastructure.Persistence;
using TalentBridge.Jobs.Domain.Aggregates;
using TalentBridge.Jobs.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var identityDb = sp.GetRequiredService<IdentityDbContext>();
        var jobsDb = sp.GetRequiredService<JobsDbContext>();
        var appsDb = sp.GetRequiredService<ApplicationsDbContext>();
        var companyDb = sp.GetRequiredService<CompanyDbContext>();

        if (await identityDb.Users.AnyAsync())
        {
            logger.LogInformation("[Seed] Data already exists — skipping.");
            return;
        }

        logger.LogInformation("[Seed] Seeding demo data...");

        // ── Users ────────────────────────────────────────────────────────────────
        var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234");
        var hrHash = BCrypt.Net.BCrypt.HashPassword("HR@1234");
        var candidateHash = BCrypt.Net.BCrypt.HashPassword("Candidate@1234");

        var admin = User.Create("admin@talentbridge.com", adminHash, UserRole.Admin, "System Admin").Value!;
        var hr = User.Create("hr@talentbridge.com", hrHash, UserRole.CompanyHR, "Sarah HR Manager").Value!;
        var candidate = User.Create("candidate@talentbridge.com", candidateHash, UserRole.Candidate, "John Candidate").Value!;

        identityDb.Users.AddRange(admin, hr, candidate);
        await identityDb.SaveChangesAsync();
        logger.LogInformation("[Seed] Created 3 users (admin, hr, candidate).");

        // ── Company ──────────────────────────────────────────────────────────────
        var company = Company.Create("TechCorp Solutions", "Leading software development company", hr.Id).Value!;
        company.Approve(admin.Id);
        companyDb.Companies.Add(company);
        await companyDb.SaveChangesAsync();
        logger.LogInformation("[Seed] Created company: TechCorp Solutions.");

        // ── Jobs ─────────────────────────────────────────────────────────────────
        var job1 = Job.Create("Senior .NET Developer", "Build scalable enterprise APIs using .NET 10, Clean Architecture, and Azure.", company.Id, hr.Id, 80000, 120000, "Pune, India").Value!;
        job1.Publish();

        var job2 = Job.Create("Angular Frontend Engineer", "Develop modern SPAs using Angular 20 with standalone components and Angular Material.", company.Id, hr.Id, 60000, 90000, "Bangalore, India").Value!;
        job2.Publish();

        var job3 = Job.Create("DevOps Engineer", "Manage Azure infrastructure using Bicep IaC, GitHub Actions CI/CD, and AKS.", company.Id, hr.Id, 70000, 110000, "Remote").Value!;
        job3.Publish();

        jobsDb.Jobs.AddRange(job1, job2, job3);
        await jobsDb.SaveChangesAsync();
        logger.LogInformation("[Seed] Created 3 published jobs.");

        // ── Applications ─────────────────────────────────────────────────────────
        var app1 = JobApplication.Create(candidate.Id, job1.Id, "I have 5 years of .NET experience and have built modular monoliths at scale.", "https://placeholder.blob.core.windows.net/resumes/demo-resume.pdf").Value!;
        app1.StartReview(hr.Id);

        var app2 = JobApplication.Create(candidate.Id, job2.Id, "Angular has been my primary stack for 3 years, including large-scale SPAs.", "https://placeholder.blob.core.windows.net/resumes/demo-resume.pdf").Value!;

        appsDb.JobApplications.AddRange(app1, app2);
        await appsDb.SaveChangesAsync();
        logger.LogInformation("[Seed] Created 2 sample applications. Seed complete.");
    }
}
