using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TalentBridge.Integration.Tests.Fixtures;

namespace TalentBridge.Integration.Tests.Tests;

public class IdentityEndpointTests : IClassFixture<TalentBridgeWebFactory>
{
    private readonly HttpClient _client;

    public IdentityEndpointTests(TalentBridgeWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/identity/register", new
        {
            email = "register@test.com",
            password = "Test@1234",
            fullName = "Register User",
            role = "Candidate"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var payload = new
        {
            email = "duplicate@test.com",
            password = "Test@1234",
            fullName = "Duplicate User",
            role = "Candidate"
        };

        await _client.PostAsJsonAsync("/api/identity/register", payload);
        var second = await _client.PostAsJsonAsync("/api/identity/register", payload);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsJwtToken()
    {
        await _client.PostAsJsonAsync("/api/identity/register", new
        {
            email = "login@test.com",
            password = "Test@1234",
            fullName = "Login User",
            role = "Candidate"
        });

        var response = await _client.PostAsJsonAsync("/api/identity/login", new
        {
            email = "login@test.com",
            password = "Test@1234"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/identity/login", new
        {
            email = "nobody@test.com",
            password = "Wrong@1234"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
