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
            var studentUser = new User { Username = "student1", Email = "student1@test.local", PasswordHash = passwordHasher.HashPassword("Student123!"), FullName = "Student One", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.Users.Add(studentUser);
            await db.SaveChangesAsync();
            if (studentRole != null) db.UserRoles.Add(new UserRole { UserId = studentUser.Id, RoleId = studentRole.Id, AssignedAt = DateTime.UtcNow });

            if (!db.Students.Any(s => s.UserId == studentUser.Id))
                db.Students.Add(new Student { UserId = studentUser.Id, StudentCode = "STU001", RollNumber = "R001" });
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
    }
}
