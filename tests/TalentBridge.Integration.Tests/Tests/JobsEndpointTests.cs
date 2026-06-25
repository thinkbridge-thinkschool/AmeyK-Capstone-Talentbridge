using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TalentBridge.Integration.Tests.Fixtures;

namespace TalentBridge.Integration.Tests.Tests;

public class JobsEndpointTests : IClassFixture<TalentBridgeWebFactory>
{
    private readonly HttpClient _client;

    public JobsEndpointTests(TalentBridgeWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetJobs_NoAuth_Returns200()
    {
        // Search is the public listing endpoint — no auth required
        var response = await _client.GetAsync("/api/jobs/search");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostJob_NoAuth_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/jobs", new
        {
            title = "Test Job",
            description = "Test Description",
            location = "Pune",
            companyId = Guid.NewGuid(),
            postedByHRId = Guid.NewGuid(),
            salaryMin = 500000m,
            salaryMax = 900000m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostJob_AsHR_Returns201Created()
    {
        var (token, hrId) = await RegisterAndLoginHRAsync("hr.jobs@test.com", "HR@1234", "HR Jobs User");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/jobs", new
        {
            title = "HR Test Job",
            description = "Posted by HR in integration test",
            location = "Mumbai",
            companyId = Guid.NewGuid(),
            postedByHRId = hrId,
            salaryMin = 800000m,
            salaryMax = 1200000m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("jobId").ToString().Should().NotBeNullOrEmpty();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<(string Token, Guid UserId)> RegisterAndLoginHRAsync(
        string email, string password, string fullName)
    {
        await _client.PostAsJsonAsync("/api/identity/register", new
        {
            email,
            password,
            fullName,
            role = "CompanyHR"
        });

        var login = await _client.PostAsJsonAsync("/api/identity/login",
            new { email, password });
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("token").GetString()!;

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        var me = await _client.GetFromJsonAsync<JsonElement>("/api/identity/me");
        _client.DefaultRequestHeaders.Authorization = null;

        var userId = Guid.Parse(me.GetProperty("id").GetString()!);
        return (token, userId);
    }
}
