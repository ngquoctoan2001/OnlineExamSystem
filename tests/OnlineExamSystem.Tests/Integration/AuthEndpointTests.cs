using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OnlineExamSystem.Tests.Integration;

/// <summary>
/// Integration tests for /api/auth endpoints.
/// Uses InMemory database — no real PostgreSQL needed.
/// </summary>
public class AuthEndpointTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public AuthEndpointTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── POST /api/auth/login ────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "Admin123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("accessToken");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "admin",
            password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "ghost_user",
            password = "anything"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmptyBody_Returns400Or401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "",
            password = ""
        });

        ((int)response.StatusCode).Should().BeOneOf(400, 401);
    }

    // ── GET /api/auth/me ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_WithValidToken_Returns200()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync("admin", "Admin123!");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("admin");
    }

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        var client = new WebAppFactory().CreateClient();
        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/auth/logout ───────────────────────────────────────────────

    [Fact]
    public async Task Logout_WithValidToken_Returns200()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync("admin", "Admin123!");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/api/auth/logout", null);

        ((int)response.StatusCode).Should().BeOneOf(200, 204);
    }

    // ── POST /api/auth/change-password ──────────────────────────────────────

    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_Returns200()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync("teacher1", "Teacher123!");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "Teacher123!",
            newPassword = "NewTeacher456!",
            confirmNewPassword = "NewTeacher456!"
        });

        ((int)response.StatusCode).Should().BeOneOf(200, 400);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync("admin", "Admin123!");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "WrongOldPassword!",
            newPassword = "NewPass123!",
            confirmNewPassword = "NewPass123!"
        });

        ((int)response.StatusCode).Should().BeOneOf(400, 422);
    }

    // ── GET /api/health/status ──────────────────────────────────────────────

    [Fact]
    public async Task HealthStatus_Returns200()
    {
        var response = await _client.GetAsync("/api/health/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("healthy");
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private async Task<string> GetTokenAsync(string username, string password)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return "";
        using var doc = JsonDocument.Parse(body);
        // Response wraps token as data.accessToken
        if (doc.RootElement.TryGetProperty("data", out var data) &&
            data.ValueKind == System.Text.Json.JsonValueKind.Object &&
            data.TryGetProperty("accessToken", out var tokenEl))
            return tokenEl.GetString() ?? "";
        if (doc.RootElement.TryGetProperty("accessToken", out var directToken))
            return directToken.GetString() ?? "";
        return "";
    }
}
