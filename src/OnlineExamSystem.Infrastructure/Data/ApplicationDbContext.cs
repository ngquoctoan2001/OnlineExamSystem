using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;

namespace OnlineExamSystem.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserSession> UserSessions { get; set; } = null!;
    public DbSet<UserLoginLog> UserLoginLogs { get; set; } = null!;
    public DbSet<School> Schools { get; set; } = null!;
    public DbSet<Subject> Subjects { get; set; } = null!;
    public DbSet<Class> Classes { get; set; } = null!;
    public DbSet<Teacher> Teachers { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<ClassTeacher> ClassTeachers { get; set; } = null!;
    public DbSet<ClassStudent> ClassStudents { get; set; } = null!;
    public DbSet<Exam> Exams { get; set; } = null!;
    public DbSet<ExamClass> ExamClasses { get; set; } = null!;
    public DbSet<ExamQuestion> ExamQuestions { get; set; } = null!;
    public DbSet<ExamSetting> ExamSettings { get; set; } = null!;
    public DbSet<ExamAttempt> ExamAttempts { get; set; } = null!;
    public DbSet<QuestionType> QuestionTypes { get; set; } = null!;
    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;
    public DbSet<Answer> Answers { get; set; } = null!;
    public DbSet<AnswerOption> AnswerOptions { get; set; } = null!;
    public DbSet<GradingResult> GradingResults { get; set; } = null!;
    public DbSet<ExamStatistic> ExamStatistics { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User relationships
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Class relationships
        modelBuilder.Entity<ClassTeacher>()
            .HasKey(ct => new { ct.ClassId, ct.TeacherId });

        modelBuilder.Entity<ClassStudent>()
            .HasKey(cs => new { cs.ClassId, cs.StudentId });

        // Exam relationships
        modelBuilder.Entity<ExamClass>()
            .HasKey(ec => new { ec.ExamId, ec.ClassId });

        modelBuilder.Entity<AnswerOption>()
            .HasKey(ao => new { ao.AnswerId, ao.OptionId });

        // Indexes for performance
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<UserSession>()
            .HasIndex(us => us.RefreshToken)
            .IsUnique();

        modelBuilder.Entity<Question>()
            .HasIndex(q => q.SubjectId);

        modelBuilder.Entity<Exam>()
            .HasIndex(e => e.CreatedBy);
    }
}
