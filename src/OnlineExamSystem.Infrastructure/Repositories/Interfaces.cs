using OnlineExamSystem.Domain.Entities;

namespace OnlineExamSystem.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(User user, CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task AssignRoleToUserAsync(long userId, long roleId, CancellationToken cancellationToken = default);
    Task RemoveRoleFromUserAsync(long userId, long roleId, CancellationToken cancellationToken = default);
    Task<User?> GetUserWithRolesAsync(long userId, CancellationToken cancellationToken = default);
    Task<(List<User> Users, int Total)> GetAllUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<User?> UpdateUserAsync(long userId, string email, string fullName, bool isActive, CancellationToken cancellationToken = default);
    Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);
}

public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<UserSession?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> DeleteByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}

public interface IUserLoginLogRepository
{
    Task<UserLoginLog> CreateAsync(UserLoginLog log, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserLoginLog>> GetByUserIdAsync(long userId, int limit = 10, CancellationToken cancellationToken = default);
}

public interface ITeacherRepository
{
    Task<Teacher?> GetByIdAsync(long id);
    Task<Teacher?> GetByUserIdAsync(long userId);
    Task<Teacher?> GetByEmployeeIdAsync(string employeeId);
    Task<(List<Teacher> Teachers, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<Teacher>> SearchAsync(string searchTerm);
    Task<List<ClassTeacher>> GetTeacherClassesAsync(long teacherId);
    Task<Teacher> CreateAsync(Teacher teacher);
    Task<Teacher> UpdateAsync(Teacher teacher);
    Task<bool> DeleteAsync(long id);
    Task<bool> EmployeeIdExistsAsync(string employeeId, long? excludeTeacherId = null);
}

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(long id);
    Task<Student?> GetByUserIdAsync(long userId);
    Task<Student?> GetByStudentCodeAsync(string studentCode);
    Task<(List<Student> Students, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<Student>> SearchAsync(string searchTerm);
    Task<List<ClassStudent>> GetStudentClassesAsync(long studentId);
    Task<Student> CreateAsync(Student student);
    Task<Student> UpdateAsync(Student student);
    Task<bool> DeleteAsync(long id);
    Task<bool> StudentCodeExistsAsync(string studentCode, long? excludeStudentId = null);
}

public interface IClassRepository
{
    Task<Class?> GetByIdAsync(long id);
    Task<Class?> GetByCodeAsync(string code);
    Task<(List<Class> Classes, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);
    Task<(List<Class> Classes, int TotalCount)> GetBySchoolAsync(long schoolId, int page = 1, int pageSize = 20);
    Task<(List<Class> Classes, int TotalCount)> GetByGradeAsync(int grade, int page = 1, int pageSize = 20);
    Task<List<Class>> SearchAsync(string searchTerm);
    Task<Class> CreateAsync(Class @class);
    Task<Class> UpdateAsync(Class @class);
    Task<bool> DeleteAsync(long id);
    Task<bool> CodeExistsAsync(string code, long? excludeClassId = null);
    Task<List<ClassStudent>> GetClassStudentsAsync(long classId);
    Task<bool> AddStudentToClassAsync(long classId, long studentId);
    Task<bool> RemoveStudentFromClassAsync(long classId, long studentId);
    Task<bool> StudentEnrolledInClassAsync(long classId, long studentId);
}

public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(long id);
    Task<Subject?> GetByCodeAsync(string code);
    Task<(List<Subject> Subjects, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<Subject>> SearchAsync(string searchTerm);
    Task<Subject> CreateAsync(Subject subject);
    Task<Subject> UpdateAsync(Subject subject);
    Task<bool> DeleteAsync(long id);
    Task<bool> CodeExistsAsync(string code, long? excludeSubjectId = null);
}

public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(long id);
    Task<(List<Question> Questions, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<Question>> SearchAsync(string searchTerm);
    Task<List<Question>> GetBySubjectAsync(long subjectId);
    Task<List<Question>> GetByDifficultyAsync(string difficulty);
    Task<List<Question>> GetPublishedAsync(int page = 1, int pageSize = 20);
    Task<List<Question>> GetByQuestionTypeAsync(long questionTypeId);
    Task<Question> CreateAsync(Question question);
    Task<Question> UpdateAsync(Question question);
    Task<bool> DeleteAsync(long id);
    Task<int> GetTotalCountAsync();
    Task<int> GetPublishedCountAsync();
}

public interface IQuestionOptionRepository
{
    Task<QuestionOption?> GetByIdAsync(long id);
    Task<List<QuestionOption>> GetByQuestionIdAsync(long questionId);
    Task<List<QuestionOption>> GetAllAsync();
    Task<QuestionOption?> GetCorrectOptionAsync(long questionId);
    Task<List<QuestionOption>> GetCorrectOptionsAsync(List<long> questionIds);
    Task<QuestionOption> CreateAsync(QuestionOption option);
    Task<List<QuestionOption>> CreateBatchAsync(List<QuestionOption> options);
    Task<QuestionOption> UpdateAsync(QuestionOption option);
    Task<bool> DeleteAsync(long id);
    Task<bool> DeleteByQuestionIdAsync(long questionId);
    Task<int> GetCountByQuestionIdAsync(long questionId);
}

public interface ITeachingAssignmentRepository
{
    Task<ClassTeacher?> GetByIdAsync(long id);
    Task<(List<ClassTeacher> Assignments, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<ClassTeacher>> GetByClassAsync(long classId);
    Task<List<ClassTeacher>> GetByTeacherAsync(long teacherId);
    Task<List<ClassTeacher>> GetBySubjectAsync(long subjectId);
    Task<ClassTeacher?> GetByClassTeacherSubjectAsync(long classId, long teacherId, long subjectId);
    Task<List<ClassTeacher>> SearchAsync(string searchTerm);
    Task<ClassTeacher> CreateAsync(ClassTeacher assignment);
    Task<ClassTeacher> UpdateAsync(ClassTeacher assignment);
    Task<bool> DeleteAsync(long id);
    Task<bool> AssignmentExistsAsync(long classId, long teacherId, long subjectId, long? excludeId = null);
}

public interface IExamRepository
{
    Task<Exam?> GetByIdAsync(long id);
    Task<(List<Exam> Exams, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<Exam>> SearchAsync(string searchTerm);
    Task<List<Exam>> GetByTeacherAsync(long teacherId);
    Task<List<Exam>> GetBySubjectAsync(long subjectId);
    Task<Exam> CreateAsync(Exam exam);
    Task<Exam> UpdateAsync(Exam exam);
    Task<bool> DeleteAsync(long id);
    Task<bool> TitleExistsAsync(string title, long? excludeExamId = null);
}

public interface IExamClassRepository
{
    Task<ExamClass?> GetByIdAsync(long examId, long classId);
    Task<List<ExamClass>> GetExamClassesAsync(long examId);
    Task<List<ExamClass>> GetClassExamsAsync(long classId);
    Task<(List<ExamClass> ExamClasses, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);
    Task<ExamClass> CreateAsync(ExamClass examClass);
    Task<bool> DeleteAsync(long examId, long classId);
    Task<bool> ExistsAsync(long examId, long classId);
    Task<int> GetClassCountForExamAsync(long examId);
    Task<int> GetExamCountForClassAsync(long classId);
}

public interface IExamAttemptRepository
{
    Task<ExamAttempt?> GetByIdAsync(long id);
    Task<(List<ExamAttempt> Attempts, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);
    Task<List<ExamAttempt>> GetStudentAttemptsAsync(long studentId);
    Task<List<ExamAttempt>> GetExamAttemptsAsync(long examId);
    Task<ExamAttempt?> GetStudentExamAttemptAsync(long studentId, long examId);
    Task<ExamAttempt> CreateAsync(ExamAttempt attempt);
    Task<ExamAttempt> UpdateAsync(ExamAttempt attempt);
    Task<bool> DeleteAsync(long id);
}

public interface IExamSettingsRepository
{
    Task<ExamSetting?> GetByExamIdAsync(long examId);
    Task<ExamSetting> CreateAsync(ExamSetting settings);
    Task<ExamSetting> UpdateAsync(ExamSetting settings);
    Task<bool> DeleteAsync(long examId);
}

public interface IExamQuestionRepository
{
    Task<ExamQuestion?> GetByIdAsync(long id);
    Task<List<ExamQuestion>> GetExamQuestionsAsync(long examId);
    Task<List<ExamQuestion>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<ExamQuestion?> GetExamQuestionAsync(long examId, long questionId);
    Task<ExamQuestion> CreateAsync(ExamQuestion examQuestion);
    Task<ExamQuestion> UpdateAsync(ExamQuestion examQuestion);
    Task<bool> DeleteAsync(long id);
    Task<bool> DeleteExamQuestionAsync(long examId, long questionId);
    Task<int> GetQuestionCountForExamAsync(long examId);
    Task<bool> ExistsAsync(long examId, long questionId);
    Task<int> GetMaxOrderAsync(long examId);
    Task<int> GetTotalCountAsync();
}

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(long id);
    Task<Tag?> GetByNameAsync(string name);
    Task<List<Tag>> GetAllAsync();
    Task<List<Tag>> SearchAsync(string searchTerm);
    Task<Tag> CreateAsync(Tag tag);
    Task<Tag> UpdateAsync(Tag tag);
    Task<bool> DeleteAsync(long id);
    Task<bool> NameExistsAsync(string name, long? excludeId = null);

    // Question-Tag assignments
    Task<List<Tag>> GetTagsByQuestionAsync(long questionId);
    Task<List<Question>> GetQuestionsByTagAsync(long tagId);
    Task<bool> AssignTagToQuestionAsync(long questionId, long tagId);
    Task<bool> RemoveTagFromQuestionAsync(long questionId, long tagId);
    Task<bool> IsTagAssignedToQuestionAsync(long questionId, long tagId);
}

public interface IAnswerRepository
{
    Task<Answer?> GetByAttemptAndQuestionAsync(long attemptId, long questionId);
    Task<List<Answer>> GetByAttemptIdAsync(long attemptId);
    Task<Answer> CreateAsync(Answer answer);
    Task<Answer> UpdateAsync(Answer answer);
    Task<bool> CreateAnswerOptionAsync(AnswerOption answerOption);
    Task DeleteAnswerOptionsByAnswerIdAsync(long answerId);
}

public interface IGradingResultRepository
{
    Task<GradingResult?> GetByAttemptAndQuestionAsync(long attemptId, long questionId);
    Task<List<GradingResult>> GetByAttemptIdAsync(long attemptId);
    Task<GradingResult> CreateAsync(GradingResult result);
    Task<GradingResult> UpdateAsync(GradingResult result);
    Task<List<GradingResult>> GetByExamIdAsync(long examId);
}

public interface IExamStatisticRepository
{
    Task<ExamStatistic?> GetByExamIdAsync(long examId);
    Task<ExamStatistic> CreateAsync(ExamStatistic stat);
    Task<ExamStatistic> UpdateOrCreateAsync(ExamStatistic stat);
}

public interface IExamViolationRepository
{
    Task<List<ExamViolation>> GetByAttemptIdAsync(long attemptId);
    Task<ExamViolation> CreateAsync(ExamViolation violation);
}

public interface INotificationRepository
{
    Task<List<Notification>> GetByUserIdAsync(long userId, bool? unreadOnly = null);
    Task<Notification?> GetByIdAsync(long id);
    Task<Notification> CreateAsync(Notification notification);
    Task<bool> DeleteAsync(long id);
    Task<bool> MarkAsReadAsync(long id);
    Task<bool> MarkAllAsReadAsync(long userId);
    Task<int> GetUnreadCountAsync(long userId);
}

public interface IActivityLogRepository
{
    Task<ActivityLog> CreateAsync(ActivityLog log);
    Task<(List<ActivityLog> Logs, int TotalCount)> GetAllAsync(int page, int pageSize, string? action = null, long? userId = null, DateTime? from = null, DateTime? to = null);
}

public interface ISubjectExamTypeRepository
{
    Task<SubjectExamType?> GetByIdAsync(long id);
    Task<List<SubjectExamType>> GetBySubjectIdAsync(long subjectId);
    Task<SubjectExamType> CreateAsync(SubjectExamType entity);
    Task<SubjectExamType> UpdateAsync(SubjectExamType entity);
    Task<bool> DeleteAsync(long id);
}
