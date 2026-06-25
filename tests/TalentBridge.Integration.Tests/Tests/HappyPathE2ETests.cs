using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TalentBridge.Integration.Tests.Fixtures;

namespace TalentBridge.Integration.Tests.Tests;

/// <summary>
/// E2E: HR registers → posts job → Candidate registers → applies
///      → HR moves to UnderReview → HR shortlists → Candidate sees Shortlisted status
/// </summary>
public class HappyPathE2ETests : IClassFixture<TalentBridgeWebFactory>
{
    private readonly HttpClient _client;

    public HappyPathE2ETests(TalentBridgeWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullHiringFlow_CandidateApplied_HRShortlists_StatusUpdates()
    {
        // ── 1. Register HR ───────────────────────────────────────────────────
        await _client.PostAsJsonAsync("/api/identity/register", new
        {
            email = "hr.e2e@test.com",
            password = "HR@1234",
            fullName = "E2E HR",
            role = "CompanyHR"
        });

        // ── 2. HR logs in ────────────────────────────────────────────────────
        var hrLogin = await _client.PostAsJsonAsync("/api/identity/login",
            new { email = "hr.e2e@test.com", password = "HR@1234" });
        var hrToken = (await hrLogin.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("token").GetString()!;

        // ── 3. HR gets their userId ──────────────────────────────────────────
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", hrToken);
        var hrMe = await _client.GetFromJsonAsync<JsonElement>("/api/identity/me");
        var hrId = Guid.Parse(hrMe.GetProperty("id").GetString()!);

        // ── 4. HR posts a job ────────────────────────────────────────────────
        var jobResp = await _client.PostAsJsonAsync("/api/jobs", new
        {
            title = "E2E Backend Developer",
            description = ".NET 10 developer needed for enterprise platform",
            location = "Pune",
            companyId = Guid.NewGuid(),
            postedByHRId = hrId,
            salaryMin = 800000m,
            salaryMax = 1200000m
        });

        jobResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var jobId = (await jobResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("jobId").GetString()!;

        // ── 5. Register Candidate ────────────────────────────────────────────
        _client.DefaultRequestHeaders.Authorization = null;

        await _client.PostAsJsonAsync("/api/identity/register", new
        {
            email = "candidate.e2e@test.com",
            password = "C@1234",
            fullName = "E2E Candidate",
            role = "Candidate"
        });

        // ── 6. Candidate logs in ─────────────────────────────────────────────
        var candidateLogin = await _client.PostAsJsonAsync("/api/identity/login",
            new { email = "candidate.e2e@test.com", password = "C@1234" });
        var candidateToken = (await candidateLogin.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("token").GetString()!;

        // ── 7. Candidate gets their userId ───────────────────────────────────
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", candidateToken);
        var candidateMe = await _client.GetFromJsonAsync<JsonElement>("/api/identity/me");
        var candidateId = Guid.Parse(candidateMe.GetProperty("id").GetString()!);

        // ── 8. Candidate searches jobs (public endpoint) ─────────────────────
        _client.DefaultRequestHeaders.Authorization = null;
        var jobs = await _client.GetAsync("/api/jobs/search");
        jobs.StatusCode.Should().Be(HttpStatusCode.OK);

        // ── 9. Candidate applies ─────────────────────────────────────────────
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", candidateToken);

        var applyResp = await _client.PostAsJsonAsync("/api/applications", new
        {
            jobId = Guid.Parse(jobId),
            candidateId,
            coverLetter = "I am very interested in this E2E Backend Developer role.",
            resumeUrl = "https://example.com/resume.pdf"
        });

        applyResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var applicationId = (await applyResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("applicationId").GetString()!;

        // ── 10. HR moves application to UnderReview ──────────────────────────
        // (Shortlisted requires UnderReview first — domain rule)
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", hrToken);

        var reviewResp = await _client.PatchAsJsonAsync(
            $"/api/applications/{applicationId}/status",
            new { NewStatus = "UnderReview" });

        reviewResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // ── 11. HR shortlists the candidate ──────────────────────────────────
        var shortlistResp = await _client.PatchAsJsonAsync(
            $"/api/applications/{applicationId}/status",
            new { NewStatus = "Shortlisted" });

        shortlistResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // ── 12. Candidate checks their application status ─────────────────────
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", candidateToken);

        var myApps = await _client.GetAsync("/api/applications/my");
        myApps.StatusCode.Should().Be(HttpStatusCode.OK);

        var appsJson = await myApps.Content.ReadFromJsonAsync<JsonElement>();
        var status = appsJson.EnumerateArray()
            .First(a => a.GetProperty("id").GetString() == applicationId)
            .GetProperty("status").GetString();

        status.Should().Be("Shortlisted");
    }
}
