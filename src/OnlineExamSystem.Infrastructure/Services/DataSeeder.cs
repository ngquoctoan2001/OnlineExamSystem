using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace OnlineExamSystem.Infrastructure.Services;

public interface IDataSeeder
{
    Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default);
}

public class DataSeeder : IDataSeeder
{
    private readonly ILogger<DataSeeder> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public DataSeeder(ILogger<DataSeeder> logger, IPasswordHasher passwordHasher)
    {
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Seed Roles
            await SeedRolesAsync(context, cancellationToken);

            // Seed Permissions
            await SeedPermissionsAsync(context, cancellationToken);

            // Save roles & permissions so they can be queried by subsequent seeds
            await context.SaveChangesAsync(cancellationToken);

            // Seed Role-Permission Mappings
            await SeedRolePermissionsAsync(context, cancellationToken);

            // Seed Question Types
            await SeedQuestionTypesAsync(context, cancellationToken);

            // Seed default admin user
            await SeedDefaultAdminAsync(context, cancellationToken);

            // Seed default school
            await SeedDefaultSchoolAsync(context, cancellationToken);

            // Seed fixed subjects
            await SeedSubjectsAsync(context, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            // Seed sample teacher (depends on school + subjects being saved above)
            await SeedSampleTeacherAsync(context, cancellationToken);

            // Seed sample student
            await SeedSampleStudentAsync(context, cancellationToken);

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private async Task SeedRolesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Roles.AnyAsync(cancellationToken))
            return;

        var roles = new[]
        {
            new Role { Name = "ADMIN", Description = "Administrator with full access" },
            new Role { Name = "TEACHER", Description = "Teacher who can create and manage exams" },
            new Role { Name = "STUDENT", Description = "Student who can take exams" }
        };

        await context.Roles.AddRangeAsync(roles, cancellationToken);
        _logger.LogInformation("Seeded {Count} roles", roles.Length);
    }

    private async Task SeedPermissionsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Permissions.AnyAsync(cancellationToken))
            return;

        var permissions = new[]
        {
            // User Management
            new Permission { Name = "users.view", Description = "View users" },
            new Permission { Name = "users.create", Description = "Create users" },
            new Permission { Name = "users.edit", Description = "Edit users" },
            new Permission { Name = "users.delete", Description = "Delete users" },

            // Teacher Management
            new Permission { Name = "teachers.view", Description = "View teachers" },
            new Permission { Name = "teachers.create", Description = "Create teachers" },
            new Permission { Name = "teachers.edit", Description = "Edit teachers" },
            new Permission { Name = "teachers.delete", Description = "Delete teachers" },

            // Student Management
            new Permission { Name = "students.view", Description = "View students" },
            new Permission { Name = "students.create", Description = "Create students" },
            new Permission { Name = "students.edit", Description = "Edit students" },
            new Permission { Name = "students.delete", Description = "Delete students" },

            // Class Management
            new Permission { Name = "classes.view", Description = "View classes" },
            new Permission { Name = "classes.create", Description = "Create classes" },
            new Permission { Name = "classes.edit", Description = "Edit classes" },
            new Permission { Name = "classes.delete", Description = "Delete classes" },

            // Subject Management
            new Permission { Name = "subjects.view", Description = "View subjects" },
            new Permission { Name = "subjects.create", Description = "Create subjects" },
            new Permission { Name = "subjects.edit", Description = "Edit subjects" },
            new Permission { Name = "subjects.delete", Description = "Delete subjects" },

            // Exam Management
            new Permission { Name = "exams.view", Description = "View exams" },
            new Permission { Name = "exams.create", Description = "Create exams" },
            new Permission { Name = "exams.edit", Description = "Edit exams" },
            new Permission { Name = "exams.delete", Description = "Delete exams" },
            new Permission { Name = "exams.grade", Description = "Grade exams" },

            // Question Management
            new Permission { Name = "questions.view", Description = "View questions" },
            new Permission { Name = "questions.create", Description = "Create questions" },
            new Permission { Name = "questions.edit", Description = "Edit questions" },
            new Permission { Name = "questions.delete", Description = "Delete questions" }
        };

        await context.Permissions.AddRangeAsync(permissions, cancellationToken);
        _logger.LogInformation("Seeded {Count} permissions", permissions.Length);
    }

    private async Task SeedRolePermissionsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.RolePermissions.AnyAsync(cancellationToken))
            return;

        var roles = await context.Roles.ToListAsync(cancellationToken);
        var permissions = await context.Permissions.ToListAsync(cancellationToken);

        var adminRole = roles.FirstOrDefault(r => r.Name == "ADMIN");
        var teacherRole = roles.FirstOrDefault(r => r.Name == "TEACHER");
        var studentRole = roles.FirstOrDefault(r => r.Name == "STUDENT");

        if (adminRole == null || teacherRole == null || studentRole == null)
            return;

        var rolePermissions = new List<RolePermission>();

        // ADMIN: All permissions
        foreach (var permission in permissions)
        {
            rolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }

        // TEACHER: Can manage exams, questions, view students
        var teacherPermNames = new[]
        {
            "exams.view", "exams.create", "exams.edit", "exams.delete", "exams.grade",
            "questions.view", "questions.create", "questions.edit", "questions.delete",
            "students.view", "classes.view", "subjects.view"
        };

        var teacherPerms = permissions.Where(p => teacherPermNames.Contains(p.Name)).ToList();
        foreach (var permission in teacherPerms)
        {
            rolePermissions.Add(new RolePermission
            {
                RoleId = teacherRole.Id,
                PermissionId = permission.Id
            });
        }

        // STUDENT: Can view exams and subjects only
        var studentPermNames = new[] { "exams.view", "subjects.view", "classes.view" };
        var studentPerms = permissions.Where(p => studentPermNames.Contains(p.Name)).ToList();
        foreach (var permission in studentPerms)
        {
            rolePermissions.Add(new RolePermission
            {
                RoleId = studentRole.Id,
                PermissionId = permission.Id
            });
        }

        await context.RolePermissions.AddRangeAsync(rolePermissions, cancellationToken);
        _logger.LogInformation("Seeded {Count} role-permission mappings", rolePermissions.Count);
    }

    private async Task SeedDefaultAdminAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Username == "admin", cancellationToken))
            return;

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "ADMIN", cancellationToken);
        if (adminRole == null) return;

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

        await context.Users.AddAsync(adminUser, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await context.UserRoles.AddAsync(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);

        _logger.LogInformation("Default admin user created: admin / Admin123!@");
    }

    private async Task SeedQuestionTypesAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.QuestionTypes.AnyAsync(cancellationToken))
            return;

        var questionTypes = new[]
        {
            new QuestionType { Name = "MCQ", Description = "Multiple Choice Question - single or multiple correct answers" },
            new QuestionType { Name = "TRUE_FALSE", Description = "True or False question" },
            new QuestionType { Name = "SHORT_ANSWER", Description = "Short text answer question" },
            new QuestionType { Name = "ESSAY", Description = "Long-form essay answer question" },
            new QuestionType { Name = "DRAWING", Description = "Drawing/Canvas answer question" }
        };

        await context.QuestionTypes.AddRangeAsync(questionTypes, cancellationToken);
        _logger.LogInformation("Seeded {Count} question types", questionTypes.Length);
    }

    private async Task SeedSubjectsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Subjects.AnyAsync(cancellationToken))
            return;

        var subjects = new[]
        {
            new Subject { Name = "Toán học", Code = "TOAN", Description = "Môn Toán học" },
            new Subject { Name = "Hóa học", Code = "HOA", Description = "Môn Hóa học" },
            new Subject { Name = "Vật lý", Code = "VATLY", Description = "Môn Vật lý" },
            new Subject { Name = "Sinh học", Code = "SINH", Description = "Môn Sinh học" },
            new Subject { Name = "Ngữ văn", Code = "NGUVAN", Description = "Môn Ngữ văn" },
            new Subject { Name = "Tiếng Anh", Code = "TIENGANH", Description = "Môn Tiếng Anh" },
            new Subject { Name = "Giáo dục Kinh tế & Pháp luật", Code = "GDKTPL", Description = "Môn Giáo dục Kinh tế và Pháp luật" },
            new Subject { Name = "Giáo dục Quốc phòng & An Ninh", Code = "GDQPAN", Description = "Môn Giáo dục Quốc phòng và An Ninh" },
            new Subject { Name = "Thể dục", Code = "THEDUC", Description = "Môn Thể dục" },
            new Subject { Name = "Lịch sử", Code = "LICHSU", Description = "Môn Lịch sử" },
            new Subject { Name = "Địa lý", Code = "DIALY", Description = "Môn Địa lý" },
            new Subject { Name = "Công nghệ", Code = "CONGNGHE", Description = "Môn Công nghệ" },
        };

        await context.Subjects.AddRangeAsync(subjects, cancellationToken);
        _logger.LogInformation("Seeded {Count} subjects", subjects.Length);
    }

    private async Task SeedDefaultSchoolAsync(ApplicationDbContext context, CancellationToken cancellationToken)    {
        if (await context.Schools.AnyAsync(cancellationToken))
            return;

        var school = new School
        {
            Name = "Trường THPT Mặc định",
            Address = "",
                Phone = "",
                CreatedAt = DateTime.UtcNow
        };

        await context.Schools.AddAsync(school, cancellationToken);
        _logger.LogInformation("Seeded default school");
    }

    private async Task SeedSampleTeacherAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Username == "teacher1", cancellationToken))
            return;

        var teacherRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "TEACHER", cancellationToken);
        if (teacherRole == null) return;

        var school = await context.Schools.FirstOrDefaultAsync(cancellationToken);
        if (school == null) return;

        var mathSubject = await context.Subjects.FirstOrDefaultAsync(s => s.Code == "TOAN", cancellationToken);
        if (mathSubject == null) return;

        // 1. Create user
        var teacherUser = new User
        {
            Username = "teacher1",
            Email = "teacher1@onlineexam.local",
            PasswordHash = _passwordHasher.HashPassword("Teacher123!@"),
            FullName = "Nguyễn Văn Giáo",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await context.Users.AddAsync(teacherUser, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // 2. Assign TEACHER role
        await context.UserRoles.AddAsync(new UserRole
        {
            UserId = teacherUser.Id,
            RoleId = teacherRole.Id,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);

        // 3. Create Teacher profile
        var teacher = new Teacher
        {
            UserId = teacherUser.Id,
            EmployeeId = "TC001",
            Department = "Toán học"
        };
        await context.Teachers.AddAsync(teacher, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // 4. Create class and assign teacher as homeroom
        var sampleClass = new Class
        {
            SchoolId = school.Id,
            Code = "10A1",
            Name = "Lớp 10A1",
            Grade = 10,
            HomeroomTeacherId = teacher.Id
        };
        await context.Classes.AddAsync(sampleClass, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // 5. Assign teacher to class with subject (ClassTeacher)
        var classTeacher = new ClassTeacher
        {
            ClassId = sampleClass.Id,
            TeacherId = teacher.Id,
            SubjectId = mathSubject.Id,
            AcademicYear = "2025-2026",
            Semester = 1,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            AssignedAt = DateTime.UtcNow
        };
        await context.ClassTeachers.AddAsync(classTeacher, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded sample teacher: teacher1 / Teacher123!@ assigned to class 10A1 (Toán học)");
    }

    private async Task SeedSampleStudentAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Username == "student", cancellationToken))
            return;

        var studentRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "STUDENT", cancellationToken);
        if (studentRole == null) return;

        // 1. Create user
        var studentUser = new User
        {
            Username = "student",
            Email = "student@onlineexam.local",
            PasswordHash = _passwordHasher.HashPassword("123123"),
            FullName = "Trần Văn Học Sinh",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await context.Users.AddAsync(studentUser, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // 2. Assign STUDENT role
        await context.UserRoles.AddAsync(new UserRole
        {
            UserId = studentUser.Id,
            RoleId = studentRole.Id,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);

        // 3. Create Student profile
        var student = new Student
        {
            UserId = studentUser.Id,
            StudentCode = "HS001",
            RollNumber = "01"
        };
        await context.Students.AddAsync(student, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // 4. Add student to class 10A1 if exists
        var sampleClass = await context.Classes.FirstOrDefaultAsync(c => c.Code == "10A1", cancellationToken);
        if (sampleClass != null)
        {
            await context.ClassStudents.AddAsync(new ClassStudent
            {
                ClassId = sampleClass.Id,
                StudentId = student.Id,
                EnrolledAt = DateTime.UtcNow
            }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Seeded sample student: student / 123123 (HS001) assigned to class 10A1");
    }
}
