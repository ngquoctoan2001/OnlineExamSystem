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
        // DataSeeder (run at Program.cs startup) already created Roles, Permissions, QuestionTypes.
        // We only need to add test users and related entities.
        if (db.Users.Any()) return;

        // Look up roles by name to get the auto-generated IDs from DataSeeder
        var adminRole   = db.Roles.FirstOrDefault(r => r.Name == "ADMIN");
        var teacherRole = db.Roles.FirstOrDefault(r => r.Name == "TEACHER");
        var studentRole = db.Roles.FirstOrDefault(r => r.Name == "STUDENT");

        // Users — use the same IPasswordHasher that AuthService uses for verification
        var adminUser   = new User { Username = "admin",    Email = "admin@test.local",    PasswordHash = passwordHasher.HashPassword("Admin123!"),   FullName = "Admin User",  IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var teacherUser = new User { Username = "teacher1", Email = "teacher1@test.local", PasswordHash = passwordHasher.HashPassword("Teacher123!"), FullName = "Teacher One", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var studentUser = new User { Username = "student1", Email = "student1@test.local", PasswordHash = passwordHasher.HashPassword("Student123!"), FullName = "Student One", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Users.AddRange(adminUser, teacherUser, studentUser);
        await db.SaveChangesAsync(); // flush to get auto-assigned user IDs

        // UserRoles using actual role IDs from DataSeeder
        if (adminRole   != null) db.UserRoles.Add(new UserRole { UserId = adminUser.Id,   RoleId = adminRole.Id,   AssignedAt = DateTime.UtcNow });
        if (teacherRole != null) db.UserRoles.Add(new UserRole { UserId = teacherUser.Id, RoleId = teacherRole.Id, AssignedAt = DateTime.UtcNow });
        if (studentRole != null) db.UserRoles.Add(new UserRole { UserId = studentUser.Id, RoleId = studentRole.Id, AssignedAt = DateTime.UtcNow });

        // School (required FK for Class)
        var school = new School { Name = "Test School", Address = "123 St", Phone = "0100" };
        db.Schools.Add(school);
        await db.SaveChangesAsync(); // flush to get school ID

        // Class, Teacher, Student, Subject
        db.Classes.Add(new Class   { SchoolId = school.Id, Name = "10A1", Code = "10A1", Grade = 10 });
        db.Teachers.Add(new Teacher { UserId = teacherUser.Id, EmployeeId = "EMP001", Department = "Math" });
        db.Students.Add(new Student { UserId = studentUser.Id, StudentCode = "STU001", RollNumber = "R001" });
        db.Subjects.Add(new Subject { Name = "Mathematics", Code = "MATH", Description = "Math subject" });

        await db.SaveChangesAsync();
    }
}
