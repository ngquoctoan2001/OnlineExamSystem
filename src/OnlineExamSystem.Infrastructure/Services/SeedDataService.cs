using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Service for seeding initial data into the database
/// </summary>
public interface ISeedDataService
{
    Task SeedRolesAndPermissionsAsync(CancellationToken cancellationToken = default);
    Task SeedDefaultAdminAsync(CancellationToken cancellationToken = default);
    Task SeedSubjectsAsync(CancellationToken cancellationToken = default);
}

public class SeedDataService : ISeedDataService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<SeedDataService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Seed roles and permissions into the database
    /// </summary>
    public async Task SeedRolesAndPermissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if roles already exist
            var existingRoles = await _dbContext.Roles.CountAsync(cancellationToken);
            if (existingRoles > 0)
            {
                _logger.LogInformation("Roles already seeded, skipping...");
                return;
            }

            // Define roles
            var roles = new List<Role>
            {
                new() { Name = "ADMIN", Description = "Administrator with full access" },
                new() { Name = "TEACHER", Description = "Teacher can manage exams and students" },
                new() { Name = "STUDENT", Description = "Student can take exams" }
            };

            // Define permissions
            var permissions = new List<Permission>
            {
                // User management permissions
                new() { Name = "manage_users", Description = "Can manage users" },
                new() { Name = "view_users", Description = "Can view users" },
                
                // Role management
                new() { Name = "manage_roles", Description = "Can manage roles and permissions" },
                
                // Teacher management
                new() { Name = "manage_teachers", Description = "Can manage teachers" },
                new() { Name = "view_teachers", Description = "Can view teachers" },
                
                // Student management
                new() { Name = "manage_students", Description = "Can manage students" },
                new() { Name = "view_students", Description = "Can view students" },
                
                // Class management
                new() { Name = "manage_classes", Description = "Can manage classes" },
                new() { Name = "view_classes", Description = "Can view classes" },
                
                // Subject management
                new() { Name = "manage_subjects", Description = "Can manage subjects" },
                new() { Name = "view_subjects", Description = "Can view subjects" },
                
                // Exam management
                new() { Name = "manage_exams", Description = "Can manage exams" },
                new() { Name = "view_exams", Description = "Can view exams" },
                new() { Name = "take_exams", Description = "Can take exams" },
                
                // Question management
                new() { Name = "manage_questions", Description = "Can manage questions" },
                new() { Name = "view_questions", Description = "Can view questions" },
                
                // Grading
                new() { Name = "grade_exams", Description = "Can grade exams" },
                new() { Name = "view_grades", Description = "Can view grades" },
                
                // Statistics
                new() { Name = "view_statistics", Description = "Can view statistics" }
            };

            await _dbContext.Roles.AddRangeAsync(roles, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added {RoleCount} roles", roles.Count);

            await _dbContext.Permissions.AddRangeAsync(permissions, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added {PermissionCount} permissions", permissions.Count);

            // Assign permissions to roles
            var adminRole = await _dbContext.Roles.FirstAsync(r => r.Name == "ADMIN", cancellationToken);
            var teacherRole = await _dbContext.Roles.FirstAsync(r => r.Name == "TEACHER", cancellationToken);
            var studentRole = await _dbContext.Roles.FirstAsync(r => r.Name == "STUDENT", cancellationToken);

            var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);

            // Admin gets all permissions
            var adminRolePermissions = allPermissions.Select(p => new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = p.Id
            }).ToList();

            // Teacher permissions
            var teacherPermissions = allPermissions
                .Where(p => new[] 
                { 
                    "view_students", "manage_exams", "view_exams", "manage_questions",
                    "view_questions", "grade_exams", "view_grades", "view_classes",
                    "view_subjects", "view_teachers"
                }.Contains(p.Name))
                .Select(p => new RolePermission
                {
                    RoleId = teacherRole.Id,
                    PermissionId = p.Id
                }).ToList();

            // Student permissions
            var studentPermissions = allPermissions
                .Where(p => new[] { "view_exams", "take_exams", "view_subjects", "view_grades" }
                    .Contains(p.Name))
                .Select(p => new RolePermission
                {
                    RoleId = studentRole.Id,
                    PermissionId = p.Id
                }).ToList();

            await _dbContext.RolePermissions.AddRangeAsync(adminRolePermissions, cancellationToken);
            await _dbContext.RolePermissions.AddRangeAsync(teacherPermissions, cancellationToken);
            await _dbContext.RolePermissions.AddRangeAsync(studentPermissions, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Roles and permissions seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding roles and permissions");
            throw;
        }
    }

    /// <summary>
    /// Seed a default admin user for initial setup
    /// </summary>
    public async Task SeedDefaultAdminAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if default admin already exists
            var existingAdmin = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == "admin", cancellationToken);
            if (existingAdmin != null)
            {
                _logger.LogInformation("Default admin user already exists, skipping...");
                return;
            }

            var adminRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "ADMIN", cancellationToken);
            if (adminRole == null)
            {
                _logger.LogWarning("Admin role not found, seeding roles first...");
                await SeedRolesAndPermissionsAsync(cancellationToken);
                adminRole = await _dbContext.Roles.FirstAsync(r => r.Name == "ADMIN", cancellationToken);
            }

            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@onlineexam.local",
                PasswordHash = _passwordHasher.HashPassword("Admin123!@"),
                FullName = "System Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.Users.AddAsync(adminUser, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Assign admin role to admin user
            var userRole = new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedAt = DateTime.UtcNow
            };

            await _dbContext.UserRoles.AddAsync(userRole, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Default admin user created with username 'admin' and password 'Admin123!@'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default admin user");
            throw;
        }
    }

    /// <summary>
    /// Seed fixed subjects into the database
    /// </summary>
    public async Task SeedSubjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existingSubjects = await _dbContext.Subjects.CountAsync(cancellationToken);
            if (existingSubjects > 0)
            {
                _logger.LogInformation("Subjects already seeded, skipping...");
                return;
            }

            var subjects = new List<Subject>
            {
                new() { Name = "Toán học", Code = "TOAN", Description = "Môn Toán học" },
                new() { Name = "Hóa học", Code = "HOA", Description = "Môn Hóa học" },
                new() { Name = "Vật lý", Code = "VATLY", Description = "Môn Vật lý" },
                new() { Name = "Sinh học", Code = "SINH", Description = "Môn Sinh học" },
                new() { Name = "Ngữ văn", Code = "NGUVAN", Description = "Môn Ngữ văn" },
                new() { Name = "Tiếng Anh", Code = "TIENGANH", Description = "Môn Tiếng Anh" },
                new() { Name = "Giáo dục Kinh tế & Pháp luật", Code = "GDKTPL", Description = "Môn Giáo dục Kinh tế và Pháp luật" },
                new() { Name = "Giáo dục Quốc phòng & An Ninh", Code = "GDQPAN", Description = "Môn Giáo dục Quốc phòng và An Ninh" },
                new() { Name = "Thể dục", Code = "THEDUC", Description = "Môn Thể dục" },
                new() { Name = "Lịch sử", Code = "LICHSU", Description = "Môn Lịch sử" },
                new() { Name = "Địa lý", Code = "DIALY", Description = "Môn Địa lý" },
                new() { Name = "Công nghệ", Code = "CONGNGHE", Description = "Môn Công nghệ" },
            };

            await _dbContext.Subjects.AddRangeAsync(subjects, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded {SubjectCount} subjects successfully", subjects.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding subjects");
            throw;
        }
    }
}
