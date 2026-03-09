namespace OnlineExamSystem.Domain.Entities;

/// <summary>
/// User entity
/// </summary>
public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public virtual ICollection<UserLoginLog> UserLoginLogs { get; set; } = new List<UserLoginLog>();
}

/// <summary>
/// Role entity
/// </summary>
public class Role
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// User-Role mapping
/// </summary>
public class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}

/// <summary>
/// Permission entity
/// </summary>
public class Permission
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Role-Permission mapping
/// </summary>
public class RolePermission
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }

    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}

/// <summary>
/// User session for refresh token
/// </summary>
public class UserSession
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}

/// <summary>
/// User login log for audit trail
/// </summary>
public class UserLoginLog
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}

/// <summary>
/// School entity
/// </summary>
public class School
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}

/// <summary>
/// Subject entity
/// </summary>
public class Subject
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
}

/// <summary>
/// Class entity
/// </summary>
public class Class
{
    public long Id { get; set; }
    public long SchoolId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Grade { get; set; }

    public virtual School School { get; set; } = null!;
    public virtual ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();
    public virtual ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
    public virtual ICollection<ExamClass> ExamClasses { get; set; } = new List<ExamClass>();
}

/// <summary>
/// Teacher entity
/// </summary>
public class Teacher
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public virtual User User { get; set; } = null!;
    public virtual ICollection<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();
}

/// <summary>
/// Student entity
/// </summary>
public class Student
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;

    public virtual User User { get; set; } = null!;
    public virtual ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
}

/// <summary>
/// Class-Teacher mapping
/// </summary>
public class ClassTeacher
{
    public long Id { get; set; }
    public long ClassId { get; set; }
    public long TeacherId { get; set; }
    public long SubjectId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public int Semester { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public virtual Class Class { get; set; } = null!;
    public virtual Teacher Teacher { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
}

/// <summary>
/// Class-Student mapping
/// </summary>
public class ClassStudent
{
    public long ClassId { get; set; }
    public long StudentId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public virtual Class Class { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
}

/// <summary>
/// Question type (MCQ, Essay, etc.)
/// </summary>
public class QuestionType
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}

/// <summary>
/// Question entity
/// </summary>
public class Question
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public long QuestionTypeId { get; set; }
    public long CreatedBy { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "MEDIUM";
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Subject Subject { get; set; } = null!;
    public virtual QuestionType QuestionType { get; set; } = null!;
    public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();
    public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
}

/// <summary>
/// Question option (for MCQ)
/// </summary>
public class QuestionOption
{
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; } = false;
    public int OrderIndex { get; set; }

    public virtual Question Question { get; set; } = null!;
}

/// <summary>
/// Exam entity
/// </summary>
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
    public string Status { get; set; } = "DRAFT";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Subject Subject { get; set; } = null!;
    public virtual ExamSetting? ExamSetting { get; set; }
    public virtual ICollection<ExamClass> ExamClasses { get; set; } = new List<ExamClass>();
    public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    public virtual ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
}

/// <summary>
/// Exam-Class mapping
/// </summary>
public class ExamClass
{
    public long ExamId { get; set; }
    public long ClassId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public virtual Exam Exam { get; set; } = null!;
    public virtual Class Class { get; set; } = null!;
}

/// <summary>
/// Exam-Question mapping with ordering
/// </summary>
public class ExamQuestion
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    public int MaxScore { get; set; } = 1;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public virtual Exam Exam { get; set; } = null!;
    public virtual Question Question { get; set; } = null!;
}

/// <summary>
/// Exam settings
/// </summary>
public class ExamSetting
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public bool ShuffleQuestions { get; set; } = false;
    public bool ShuffleAnswers { get; set; } = false;
    public bool ShowResultImmediately { get; set; } = false;
    public bool AllowReview { get; set; } = true;

    public virtual Exam Exam { get; set; } = null!;
}

/// <summary>
/// Exam attempt by student
/// </summary>
public class ExamAttempt
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long StudentId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "IN_PROGRESS";
    public decimal? Score { get; set; }

    public virtual Exam Exam { get; set; } = null!;
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
}

/// <summary>
/// Student answer
/// </summary>
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
    public virtual ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
}

/// <summary>
/// Answer option (for MCQ)
/// </summary>
public class AnswerOption
{
    public long AnswerId { get; set; }
    public long OptionId { get; set; }

    public virtual Answer Answer { get; set; } = null!;
}

/// <summary>
/// Grading result
/// </summary>
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
}

/// <summary>
/// Exam statistics
/// </summary>
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
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public virtual Exam Exam { get; set; } = null!;
}
