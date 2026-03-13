using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Tests.Integration;

[Trait("Category", "GoLiveRegression")]
public class GoLiveCriticalPathRegressionTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public GoLiveCriticalPathRegressionTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CriticalPath_ProtectedEndpoints_WithoutToken_Return401()
    {
        var client = _factory.CreateClient();

        var gradingResp = await client.GetAsync("/api/grading/attempts/1/view");
        var examAttemptsResp = await client.GetAsync("/api/exam-attempts?page=1&pageSize=10");

        gradingResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        examAttemptsResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CriticalPath_GradingView_RoleMatrix_WorksAsExpected()
    {
        var attemptId = CreateSubmittedAttemptForMatrixExam();

        var studentClient = await AuthorizeAsync("student1", "Student123!");
        var teacherClient = await AuthorizeAsync("teacher1", "Teacher123!");
        var adminClient = await AuthorizeAsync("admin", "Admin123!");

        var studentResp = await studentClient.GetAsync($"/api/grading/attempts/{attemptId}/view");
        var teacherResp = await teacherClient.GetAsync($"/api/grading/attempts/{attemptId}/view");
        var adminResp = await adminClient.GetAsync($"/api/grading/attempts/{attemptId}/view");

        studentResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        teacherResp.StatusCode.Should().Be(HttpStatusCode.OK);
        adminResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CriticalPath_ExamAttemptsList_RoleMatrix_WorksAsExpected()
    {
        var studentClient = await AuthorizeAsync("student1", "Student123!");
        var teacherClient = await AuthorizeAsync("teacher1", "Teacher123!");
        var adminClient = await AuthorizeAsync("admin", "Admin123!");

        var studentResp = await studentClient.GetAsync("/api/exam-attempts?page=1&pageSize=10");
        var teacherResp = await teacherClient.GetAsync("/api/exam-attempts?page=1&pageSize=10");
        var adminResp = await adminClient.GetAsync("/api/exam-attempts?page=1&pageSize=10");

        studentResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        teacherResp.StatusCode.Should().Be(HttpStatusCode.OK);
        adminResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CriticalPath_TeacherCanFinalizeAndPublish_StudentCanViewOwnResult()
    {
        var attemptId = CreateSubmittedAttemptForMatrixExam();
        var teacherClient = await AuthorizeAsync("teacher1", "Teacher123!");
        var studentClient = await AuthorizeAsync("student1", "Student123!");

        var markResp = await teacherClient.PostAsync($"/api/grading/attempts/{attemptId}/mark-graded", null);
        markResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var publishResp = await teacherClient.PostAsync($"/api/grading/attempts/{attemptId}/publish", null);
        publishResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var studentResultResp = await studentClient.GetAsync($"/api/grading/attempts/{attemptId}/result");
        studentResultResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CriticalPath_PublishResult_CreatesNotificationForStudent()
    {
        var attemptId = CreateSubmittedAttemptForMatrixExam();
        var teacherClient = await AuthorizeAsync("teacher1", "Teacher123!");
        var matrix = GetMatrixIds();

        var markResp = await teacherClient.PostAsync($"/api/grading/attempts/{attemptId}/mark-graded", null);
        markResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var publishResp = await teacherClient.PostAsync($"/api/grading/attempts/{attemptId}/publish", null);
        publishResp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasNotification = db.Notifications.Any(n =>
            n.UserId == matrix.StudentUserId &&
            n.Type == "GRADE_PUBLISHED" &&
            n.RelatedEntityId == attemptId);

        hasNotification.Should().BeTrue();
    }

    [Fact]
    public async Task CriticalPath_StudentCannotReadAnotherStudentsResult()
    {
        var matrix = GetMatrixIds();
        var foreignAttemptId = CreateSubmittedAttemptForMatrixExam();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var anotherUser = db.Users.FirstOrDefault(u => u.Username == "student2");
            if (anotherUser == null)
            {
                anotherUser = new User
                {
                    Username = "student2",
                    Email = "student2@test.local",
                    PasswordHash = db.Users.First(u => u.Username == "student1").PasswordHash,
                    FullName = "Student Two",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Users.Add(anotherUser);
                db.SaveChanges();

                var studentRole = db.Roles.First(r => r.Name == "STUDENT");
                db.UserRoles.Add(new UserRole { UserId = anotherUser.Id, RoleId = studentRole.Id, AssignedAt = DateTime.UtcNow });
                db.Students.Add(new Student { UserId = anotherUser.Id, StudentCode = "STU002", RollNumber = "R002" });
                db.SaveChanges();
            }
        }

        var student2Client = await AuthorizeAsync("student2", "Student123!");
        var response = await student2Client.GetAsync($"/api/grading/attempts/{foreignAttemptId}/result");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private (long ExamId, long StudentId, long StudentUserId) GetMatrixIds()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var (examId, studentId, studentUserId) = EnsureMatrixData(db);
        return (examId, studentId, studentUserId);
    }

    private long CreateSubmittedAttemptForMatrixExam()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var matrix = GetMatrixIds();

        var attempt = new ExamAttempt
        {
            ExamId = matrix.ExamId,
            StudentId = matrix.StudentId,
            StartTime = DateTime.UtcNow.AddMinutes(-30),
            EndTime = DateTime.UtcNow.AddMinutes(-5),
            Status = "SUBMITTED",
            IsResultPublished = false,
            IsLateSubmission = false,
            LatePenaltyPercent = 0m
        };

        db.ExamAttempts.Add(attempt);
        db.SaveChanges();
        return attempt.Id;
    }

    private static (long ExamId, long StudentId, long StudentUserId) EnsureMatrixData(ApplicationDbContext db)
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
        return (exam.Id, student!.Id, student.UserId);
    }

    private async Task<HttpClient> AuthorizeAsync(string username, string password)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var body = await response.Content.ReadAsStringAsync();

        string token = string.Empty;
        if (!string.IsNullOrWhiteSpace(body))
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("accessToken", out var tokenEl))
            {
                token = tokenEl.GetString() ?? string.Empty;
            }
            else if (doc.RootElement.TryGetProperty("accessToken", out var directToken))
            {
                token = directToken.GetString() ?? string.Empty;
            }
        }

        token.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
