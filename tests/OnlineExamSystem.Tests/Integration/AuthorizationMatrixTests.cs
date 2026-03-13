using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Tests.Integration;

public class AuthorizationMatrixTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public AuthorizationMatrixTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GradingView_AsStudent_Returns403()
    {
        var client = await AuthorizeAsync("student1", "Student123!");
        var attemptId = GetMatrixAttemptId();

        var response = await client.GetAsync($"/api/grading/attempts/{attemptId}/view");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GradingView_AsTeacher_Returns200()
    {
        var client = await AuthorizeAsync("teacher1", "Teacher123!");
        var attemptId = GetMatrixAttemptId();

        var response = await client.GetAsync($"/api/grading/attempts/{attemptId}/view");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GradingView_AsAdmin_Returns200()
    {
        var client = await AuthorizeAsync("admin", "Admin123!");
        var attemptId = GetMatrixAttemptId();

        var response = await client.GetAsync($"/api/grading/attempts/{attemptId}/view");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExamAttempts_GetAll_AsStudent_Returns403()
    {
        var client = await AuthorizeAsync("student1", "Student123!");

        var response = await client.GetAsync("/api/exam-attempts?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("teacher1", "Teacher123!")]
    [InlineData("admin", "Admin123!")]
    public async Task ExamAttempts_GetAll_AsTeacherOrAdmin_Returns200(string username, string password)
    {
        var client = await AuthorizeAsync(username, password);

        var response = await client.GetAsync("/api/exam-attempts?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("student1", "Student123!")]
    [InlineData("admin", "Admin123!")]
    public async Task ExamAttempts_Start_AsStudentOrAdmin_IsNotForbidden(string username, string password)
    {
        var client = await AuthorizeAsync(username, password);
        var studentId = GetMatrixStudentId();

        var response = await client.PostAsJsonAsync("/api/exam-attempts/start", new
        {
            examId = 999999L,
            studentId
        });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExamAttempts_Start_AsTeacher_Returns403()
    {
        var client = await AuthorizeAsync("teacher1", "Teacher123!");
        var studentId = GetMatrixStudentId();

        var response = await client.PostAsJsonAsync("/api/exam-attempts/start", new
        {
            examId = 999999L,
            studentId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private long GetMatrixAttemptId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var (examId, studentId) = EnsureMatrixData(db);

        var attemptId = db.ExamAttempts
            .Where(a => a.ExamId == examId && a.StudentId == studentId)
            .Select(a => a.Id)
            .FirstOrDefault();

        if (attemptId <= 0)
        {
            var attempt = new ExamAttempt
            {
                ExamId = examId,
                StudentId = studentId,
                StartTime = DateTime.UtcNow.AddMinutes(-25),
                EndTime = DateTime.UtcNow.AddMinutes(-3),
                Status = "SUBMITTED",
                IsResultPublished = false,
                IsLateSubmission = false,
                LatePenaltyPercent = 0m
            };
            db.ExamAttempts.Add(attempt);
            db.SaveChanges();
            attemptId = attempt.Id;
        }

        attemptId.Should().BeGreaterThan(0);
        return attemptId;
    }

    private long GetMatrixStudentId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var (_, studentId) = EnsureMatrixData(db);

        studentId.Should().BeGreaterThan(0);
        return studentId;
    }

    private static (long ExamId, long StudentId) EnsureMatrixData(ApplicationDbContext db)
    {
        var teacher = db.Teachers.FirstOrDefault(t => t.User.Username == "teacher1");
        var student = db.Students.FirstOrDefault(s => s.User.Username == "student1");
        var mathSubject = db.Subjects.FirstOrDefault(s => s.Code == "MATH")
                         ?? db.Subjects.FirstOrDefault();

        teacher.Should().NotBeNull();
        student.Should().NotBeNull();
        mathSubject.Should().NotBeNull();

        var anyClass = db.Classes.FirstOrDefault();
        if (anyClass == null)
        {
            var school = db.Schools.FirstOrDefault();
            if (school == null)
            {
                school = new School { Name = "Test School", Address = "123 St", Phone = "0100" };
                db.Schools.Add(school);
                db.SaveChanges();
            }

            anyClass = new Class { SchoolId = school.Id, Name = "10A1", Code = "10A1", Grade = 10 };
            db.Classes.Add(anyClass);
            db.SaveChanges();
        }

        if (!db.ClassTeachers.Any(ct => ct.ClassId == anyClass.Id && ct.TeacherId == teacher!.Id && ct.SubjectId == mathSubject!.Id))
        {
            db.ClassTeachers.Add(new ClassTeacher
            {
                Id = DateTime.UtcNow.Ticks,
                ClassId = anyClass.Id,
                TeacherId = teacher.Id,
                SubjectId = mathSubject.Id,
                AcademicYear = "2025-2026",
                Semester = 2,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                AssignedAt = DateTime.UtcNow
            });
        }

        if (!db.ClassStudents.Any(cs => cs.ClassId == anyClass.Id && cs.StudentId == student!.Id))
        {
            db.ClassStudents.Add(new ClassStudent
            {
                ClassId = anyClass.Id,
                StudentId = student.Id,
                EnrolledAt = DateTime.UtcNow
            });
        }

        db.SaveChanges();

        var exam = db.Exams.FirstOrDefault(e => e.Title == "AUTH_MATRIX_EXAM");
        if (exam == null)
        {
            exam = new Exam
            {
                Title = "AUTH_MATRIX_EXAM",
                SubjectId = mathSubject!.Id,
                CreatedBy = teacher!.Id,
                DurationMinutes = 60,
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(2),
                Status = "ACTIVE",
                Description = "Seeded by integration tests",
                CreatedAt = DateTime.UtcNow
            };
            db.Exams.Add(exam);
            db.SaveChanges();
        }

        if (!db.ExamClasses.Any(ec => ec.ExamId == exam.Id && ec.ClassId == anyClass.Id))
        {
            db.ExamClasses.Add(new ExamClass
            {
                ExamId = exam.Id,
                ClassId = anyClass.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        if (!db.ExamSettings.Any(es => es.ExamId == exam.Id))
        {
            db.ExamSettings.Add(new ExamSetting
            {
                ExamId = exam.Id,
                ShuffleQuestions = false,
                ShuffleAnswers = false,
                ShowResultImmediately = false,
                AllowReview = false,
                AllowLateSubmission = true,
                GracePeriodMinutes = 15,
                LatePenaltyPercent = 10m
            });
        }

        db.SaveChanges();
        return (exam.Id, student!.Id);
    }

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
                data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("accessToken", out var tokenEl))
                token = tokenEl.GetString() ?? "";
            else if (doc.RootElement.TryGetProperty("accessToken", out var directToken))
                token = directToken.GetString() ?? "";
        }

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
