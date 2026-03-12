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
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<QuestionTag> QuestionTags { get; set; } = null!;
    public DbSet<ExamViolation> ExamViolations { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
    public DbSet<SubjectExamType> SubjectExamTypes { get; set; } = null!;

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

        modelBuilder.Entity<Class>()
            .HasOne(c => c.HomeroomTeacher)
            .WithMany()
            .HasForeignKey(c => c.HomeroomTeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        // Exam relationships
        modelBuilder.Entity<ExamClass>()
            .HasKey(ec => new { ec.ExamId, ec.ClassId });

        modelBuilder.Entity<AnswerOption>()
            .HasKey(ao => new { ao.AnswerId, ao.OptionId });

        modelBuilder.Entity<QuestionTag>()
            .HasKey(qt => new { qt.QuestionId, qt.TagId });

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

        // Additional performance indexes
        modelBuilder.Entity<Exam>()
            .HasIndex(e => e.Status);

        modelBuilder.Entity<Exam>()
            .HasIndex(e => e.SubjectId);

        modelBuilder.Entity<ExamAttempt>()
            .HasIndex(ea => ea.StudentId);

        modelBuilder.Entity<ExamAttempt>()
            .HasIndex(ea => ea.ExamId);

        modelBuilder.Entity<ExamAttempt>()
            .HasIndex(ea => new { ea.StudentId, ea.ExamId });

        modelBuilder.Entity<Answer>()
            .HasIndex(a => a.ExamAttemptId);

        modelBuilder.Entity<GradingResult>()
            .HasIndex(gr => gr.ExamAttemptId);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.UserId);

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead });

        modelBuilder.Entity<ActivityLog>()
            .HasIndex(al => al.UserId);

        modelBuilder.Entity<ActivityLog>()
            .HasIndex(al => al.Action);

        modelBuilder.Entity<ActivityLog>()
            .HasIndex(al => al.OccurredAt);

        modelBuilder.Entity<Student>()
            .HasIndex(s => s.StudentCode)
            .IsUnique();

        modelBuilder.Entity<Teacher>()
            .HasIndex(t => t.EmployeeId)
            .IsUnique();

        // SubjectExamType
        modelBuilder.Entity<SubjectExamType>()
            .HasIndex(e => e.SubjectId);

        modelBuilder.Entity<Exam>()
            .HasOne(e => e.SubjectExamType)
            .WithMany(set => set.Exams)
            .HasForeignKey(e => e.SubjectExamTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
