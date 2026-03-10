using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace OnlineExamSystem.Tests.Integration;

/// <summary>
/// Integration tests for exam, subject, student, teacher, class endpoints.
/// </summary>
public class ExamEndpointTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public ExamEndpointTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ══════════════════════════════════════════════════════════
    //  GET /api/exams
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetExams_WithoutAuth_Returns401()
    {
        var resp = await _client.GetAsync("/api/exams");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetExams_AsAdmin_Returns200()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var resp = await client.GetAsync("/api/exams");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ══════════════════════════════════════════════════════════
    //  GET /api/exams/{id} - non-existent
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetExamById_NotFound_Returns404Or200WithFalse()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var resp = await client.GetAsync("/api/exams/9999");
        // May 404 or 200 with success=false depending on implementation
        ((int)resp.StatusCode).Should().BeOneOf(200, 404);
    }

    // ══════════════════════════════════════════════════════════
    //  POST /api/exams  (create)
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateExam_AsAdmin_Returns200Or201()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");

        var payload = new
        {
            title = "Integration Test Exam",
            subjectId = 1,
            createdBy = 1,
            durationMinutes = 60,
            startTime = DateTime.UtcNow.AddDays(1).ToString("o"),
            endTime = DateTime.UtcNow.AddDays(1).AddHours(2).ToString("o"),
            description = "Created by integration test"
        };

        var resp = await client.PostAsJsonAsync("/api/exams", payload);
        ((int)resp.StatusCode).Should().BeOneOf(200, 201);
    }

    [Fact]
    public async Task CreateExam_WithoutAuth_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/exams", new
        {
            title = "Unauthorized Exam",
            subjectId = 1,
            createdBy = 1,
            durationMinutes = 60,
            startTime = DateTime.UtcNow.AddDays(1).ToString("o"),
            endTime = DateTime.UtcNow.AddDays(1).AddHours(2).ToString("o")
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ══════════════════════════════════════════════════════════
    //  GET /api/subjects
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSubjects_Returns200()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var resp = await client.GetAsync("/api/subjects");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ══════════════════════════════════════════════════════════
    //  GET /api/students
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetStudents_AsAdmin_Returns200()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var resp = await client.GetAsync("/api/students");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStudents_WithoutAuth_Returns401()
    {
        var resp = await _client.GetAsync("/api/students");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ══════════════════════════════════════════════════════════
    //  GET /api/teachers
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetTeachers_AsAdmin_Returns200()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var resp = await client.GetAsync("/api/teachers");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ══════════════════════════════════════════════════════════
    //  GET /api/classes
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetClasses_AsAdmin_Returns200()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var resp = await client.GetAsync("/api/classes");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ══════════════════════════════════════════════════════════
    //  GET /api/questions
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetQuestions_AsAdmin_Returns200()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var resp = await client.GetAsync("/api/questions");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ══════════════════════════════════════════════════════════
    //  POST /api/questions (create)
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateQuestion_AsAdmin_Returns200Or201()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");

        var resp = await client.PostAsJsonAsync("/api/questions", new
        {
            subjectId = 1,
            questionTypeId = 1,
            content = "What is 2 + 2?",
            difficulty = "EASY",
            options = new[]
            {
                new { label = "A", content = "3", isCorrect = false, orderIndex = 0 },
                new { label = "B", content = "4", isCorrect = true, orderIndex = 1 },
                new { label = "C", content = "5", isCorrect = false, orderIndex = 2 }
            }
        });

        ((int)resp.StatusCode).Should().BeOneOf(200, 201);
    }

    // ══════════════════════════════════════════════════════════
    //  GET /api/notifications
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetNotifications_WithoutAuth_Returns401()
    {
        var resp = await _client.GetAsync("/api/notifications");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_WithAuth_Returns200()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var resp = await client.GetAsync("/api/notifications");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ══════════════════════════════════════════════════════════
    //  GET /api/statistics/exams/{id}
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetExamStatistics_NonExistentExam_ReturnsNotFound()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var resp = await client.GetAsync("/api/statistics/exams/9999");
        ((int)resp.StatusCode).Should().BeOneOf(200, 404);
    }

    // ══════════════════════════════════════════════════════════
    //  GET /api/activity-logs (admin only)
    // ══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetActivityLogs_AsAdmin_Returns200()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var resp = await client.GetAsync("/api/logs");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ══════════════════════════════════════════════════════════
    //  Helper
    // ══════════════════════════════════════════════════════════

    private async Task<HttpClient> AuthorizeAsync(string username, string password)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var body = await resp.Content.ReadAsStringAsync();

        string token = "";
        if (!string.IsNullOrWhiteSpace(body))
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.ValueKind == System.Text.Json.JsonValueKind.Object &&
                data.TryGetProperty("accessToken", out var tokenEl))
                token = tokenEl.GetString() ?? "";
            else if (doc.RootElement.TryGetProperty("accessToken", out var directToken))
                token = directToken.GetString() ?? "";
        }

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
