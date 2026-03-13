using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that replaces Npgsql with InMemory EF Core.
/// Seeding is deferred to InitializeAsync so it runs after the host is built.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Remove provider-specific options configuration (holds UseNpgsql call)
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
            // Remove the resolved options instance
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(DbContextOptions));

            // Register fresh InMemory DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>Seed data AFTER the host is fully built — avoids the "two providers" conflict.</summary>
    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await db.Database.EnsureCreatedAsync();
        await SeedAsync(db, passwordHasher);
    }

    public new Task DisposeAsync() => base.DisposeAsync().AsTask();

    private static async Task SeedAsync(ApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        // DataSeeder (run at Program.cs startup) already created Roles, Permissions, QuestionTypes,
        // and a default admin user. We need to ensure test users exist with known passwords.

        // Look up roles by name to get the auto-generated IDs from DataSeeder
        var adminRole   = db.Roles.FirstOrDefault(r => r.Name == "ADMIN");
        var teacherRole = db.Roles.FirstOrDefault(r => r.Name == "TEACHER");
        var studentRole = db.Roles.FirstOrDefault(r => r.Name == "STUDENT");

        // Update existing admin user's password to match test expectations, or create if missing
        var existingAdmin = db.Users.FirstOrDefault(u => u.Username == "admin");
        if (existingAdmin != null)
        {
            existingAdmin.PasswordHash = passwordHasher.HashPassword("Admin123!");
            existingAdmin.IsActive = true;
        }
        else
        {
            existingAdmin = new User { Username = "admin", Email = "admin@test.local", PasswordHash = passwordHasher.HashPassword("Admin123!"), FullName = "Admin User", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.Users.Add(existingAdmin);
            await db.SaveChangesAsync();
            if (adminRole != null) db.UserRoles.Add(new UserRole { UserId = existingAdmin.Id, RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow });
        }

        // Ensure teacher1 exists with known test password
        var existingTeacher = db.Users.FirstOrDefault(u => u.Username == "teacher1");
        if (existingTeacher != null)
        {
            existingTeacher.PasswordHash = passwordHasher.HashPassword("Teacher123!");
            existingTeacher.IsActive = true;
        }
        else
        {
            existingTeacher = new User { Username = "teacher1", Email = "teacher1@test.local", PasswordHash = passwordHasher.HashPassword("Teacher123!"), FullName = "Teacher One", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.Users.Add(existingTeacher);
            await db.SaveChangesAsync();
            if (teacherRole != null) db.UserRoles.Add(new UserRole { UserId = existingTeacher.Id, RoleId = teacherRole.Id, AssignedAt = DateTime.UtcNow });

            if (!db.Teachers.Any(t => t.UserId == existingTeacher.Id))
                db.Teachers.Add(new Teacher { UserId = existingTeacher.Id, EmployeeId = "EMP001", Department = "Math" });
        }

        if (!db.Users.Any(u => u.Username == "student1"))
        {
            var createdStudentUser = new User { Username = "student1", Email = "student1@test.local", PasswordHash = passwordHasher.HashPassword("Student123!"), FullName = "Student One", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.Users.Add(createdStudentUser);
            await db.SaveChangesAsync();
            if (studentRole != null) db.UserRoles.Add(new UserRole { UserId = createdStudentUser.Id, RoleId = studentRole.Id, AssignedAt = DateTime.UtcNow });

            if (!db.Students.Any(s => s.UserId == createdStudentUser.Id))
                db.Students.Add(new Student { UserId = createdStudentUser.Id, StudentCode = "STU001", RollNumber = "R001" });
        }

        // Ensure school and class exist
        if (!db.Schools.Any())
        {
            var school = new School { Name = "Test School", Address = "123 St", Phone = "0100" };
            db.Schools.Add(school);
            await db.SaveChangesAsync();

            if (!db.Classes.Any())
                db.Classes.Add(new Class { SchoolId = school.Id, Name = "10A1", Code = "10A1", Grade = 10 });
        }

        if (!db.Subjects.Any(s => s.Code == "MATH"))
            db.Subjects.Add(new Subject { Name = "Mathematics", Code = "MATH", Description = "Math subject" });

        await db.SaveChangesAsync();

        // Seed deterministic data for authorization matrix tests (grading + exam-attempt endpoints).
        var teacherUser = db.Users.FirstOrDefault(u => u.Username == "teacher1");
        var studentUser = db.Users.FirstOrDefault(u => u.Username == "student1");
        if (teacherUser == null || studentUser == null)
            return;

        var teacher = db.Teachers.FirstOrDefault(t => t.UserId == teacherUser.Id);
        var student = db.Students.FirstOrDefault(s => s.UserId == studentUser.Id);
        var mathSubject = db.Subjects.FirstOrDefault(s => s.Code == "MATH");
        var anyClass = db.Classes.FirstOrDefault();

        if (teacher == null || student == null || mathSubject == null || anyClass == null)
            return;

        if (!db.ClassTeachers.Any(ct => ct.ClassId == anyClass.Id && ct.TeacherId == teacher.Id))
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

        if (!db.ClassStudents.Any(cs => cs.ClassId == anyClass.Id && cs.StudentId == student.Id))
        {
            db.ClassStudents.Add(new ClassStudent
            {
                ClassId = anyClass.Id,
                StudentId = student.Id,
                EnrolledAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        var matrixExam = db.Exams.FirstOrDefault(e => e.Title == "AUTH_MATRIX_EXAM");
        if (matrixExam == null)
        {
            matrixExam = new Exam
            {
                Title = "AUTH_MATRIX_EXAM",
                SubjectId = mathSubject.Id,
                CreatedBy = teacher.Id,
                DurationMinutes = 60,
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(2),
                Status = "ACTIVE",
                Description = "Seeded for integration authorization matrix tests",
                CreatedAt = DateTime.UtcNow
            };
            db.Exams.Add(matrixExam);
            await db.SaveChangesAsync();
        }

        if (!db.ExamClasses.Any(ec => ec.ExamId == matrixExam.Id && ec.ClassId == anyClass.Id))
        {
            db.ExamClasses.Add(new ExamClass
            {
                ExamId = matrixExam.Id,
                ClassId = anyClass.Id,
                AssignedAt = DateTime.UtcNow
            });
        }

        if (!db.ExamSettings.Any(es => es.ExamId == matrixExam.Id))
        {
            db.ExamSettings.Add(new ExamSetting
            {
                ExamId = matrixExam.Id,
                ShuffleQuestions = false,
                ShuffleAnswers = false,
                ShowResultImmediately = false,
                AllowReview = false,
                AllowLateSubmission = true,
                GracePeriodMinutes = 15,
                LatePenaltyPercent = 10m
            });
        }

        await db.SaveChangesAsync();

        if (!db.ExamAttempts.Any(a => a.ExamId == matrixExam.Id && a.StudentId == student.Id))
        {
            db.ExamAttempts.Add(new ExamAttempt
            {
                ExamId = matrixExam.Id,
                StudentId = student.Id,
                StartTime = DateTime.UtcNow.AddMinutes(-30),
                EndTime = DateTime.UtcNow.AddMinutes(-5),
                Status = "SUBMITTED",
                IsResultPublished = false,
                IsLateSubmission = false,
                LatePenaltyPercent = 0m
            });
            await db.SaveChangesAsync();
        }
    }
}
