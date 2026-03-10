namespace OnlineExamSystem.Domain.Entities;

// ── Auth & Identity ──────────────────────────────────────────────────────────

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public virtual ICollection<UserLoginLog> UserLoginLogs { get; set; } = new List<UserLoginLog>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime AssignedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

public class RolePermission
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }

    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}

public class UserSession
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

public class UserLoginLog
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }

    public virtual User User { get; set; } = null!;
}

// ── School & People ──────────────────────────────────────────────────────────

public class School
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}

public class Subject
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
}

public class Class
{
    public long Id { get; set; }
    public long SchoolId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Grade { get; set; }
    public long? HomeroomTeacherId { get; set; }

    public virtual School School { get; set; } = null!;
    public virtual Teacher? HomeroomTeacher { get; set; }
    public virtual ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();
    public virtual ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
    public virtual ICollection<ExamClass> ExamClasses { get; set; } = new List<ExamClass>();
}

public class Teacher
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public virtual User User { get; set; } = null!;
    public virtual ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();
}

public class Student
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;

    public virtual User User { get; set; } = null!;
    public virtual ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
    public virtual ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
}

public class ClassTeacher
{
    public long ClassId { get; set; }
    public long TeacherId { get; set; }
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public int Semester { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public DateTime AssignedAt { get; set; }

    public virtual Class Class { get; set; } = null!;
    public virtual Teacher Teacher { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
}

public class ClassStudent
{
    public long ClassId { get; set; }
    public long StudentId { get; set; }
    public DateTime EnrolledAt { get; set; }

    public virtual Class Class { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
}

// ── Exam ─────────────────────────────────────────────────────────────────────

public class Exam
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public long CreatedBy { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int TotalScore { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public virtual Subject Subject { get; set; } = null!;
    public virtual ICollection<ExamClass> ExamClasses { get; set; } = new List<ExamClass>();
    public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    public virtual ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
    public virtual ExamSetting? ExamSetting { get; set; }
    public virtual ICollection<ExamStatistic> ExamStatistics { get; set; } = new List<ExamStatistic>();
}

public class ExamClass
{
    public long ExamId { get; set; }
    public long ClassId { get; set; }
    public DateTime AssignedAt { get; set; }

    public virtual Exam Exam { get; set; } = null!;
    public virtual Class Class { get; set; } = null!;
}

public class ExamSetting
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public bool ShowResultImmediately { get; set; }
    public bool AllowReview { get; set; }

    public virtual Exam Exam { get; set; } = null!;
}

public class ExamAttempt
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long StudentId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Score { get; set; }
    public bool IsResultPublished { get; set; }

    public virtual Exam Exam { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public virtual ICollection<GradingResult> GradingResults { get; set; } = new List<GradingResult>();
    public virtual ICollection<ExamViolation> ExamViolations { get; set; } = new List<ExamViolation>();
}

public class ExamStatistic
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public int TotalAttempts { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal AverageScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal MinScore { get; set; }
    public DateTime CalculatedAt { get; set; }

    public virtual Exam Exam { get; set; } = null!;
}

public class ExamViolation
{
    public long Id { get; set; }
    public long ExamAttemptId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; }

    public virtual ExamAttempt ExamAttempt { get; set; } = null!;
}

// ── Question ─────────────────────────────────────────────────────────────────

public class QuestionType
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}

public class Question
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public long QuestionTypeId { get; set; }
    public long CreatedBy { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Subject Subject { get; set; } = null!;
    public virtual QuestionType QuestionType { get; set; } = null!;
    public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();
    public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
}

public class QuestionOption
{
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }

    public virtual Question Question { get; set; } = null!;
}

public class ExamQuestion
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    public int MaxScore { get; set; }
    public DateTime AddedAt { get; set; }

    public virtual Exam Exam { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
}

public class Tag
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
}

public class QuestionTag
{
    public long QuestionId { get; set; }
    public long TagId { get; set; }
    public DateTime AssignedAt { get; set; }

    public virtual Question Question { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}

// ── Answer & Grading ─────────────────────────────────────────────────────────

public class Answer
{
    public long Id { get; set; }
    public long ExamAttemptId { get; set; }
    public long QuestionId { get; set; }
    public string? TextContent { get; set; }
    public string? EssayContent { get; set; }
    public string? CanvasImage { get; set; }
    public DateTime? AnsweredAt { get; set; }

    public virtual ExamAttempt ExamAttempt { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
    public virtual ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
}

public class AnswerOption
{
    public long AnswerId { get; set; }
    public long OptionId { get; set; }

    public virtual Answer Answer { get; set; } = null!;
}

public class GradingResult
{
    public long Id { get; set; }
    public long ExamAttemptId { get; set; }
    public long QuestionId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public string? Annotations { get; set; }
    public long? GradedBy { get; set; }
    public DateTime? GradedAt { get; set; }

    public virtual ExamAttempt ExamAttempt { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
}

// ── Notifications & Logs ─────────────────────────────────────────────────────

public class Notification
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public long? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public virtual User User { get; set; } = null!;
}

public class ActivityLog
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAt { get; set; }

    public virtual User? User { get; set; }
}
