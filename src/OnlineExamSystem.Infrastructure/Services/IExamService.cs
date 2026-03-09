namespace OnlineExamSystem.Infrastructure.Services;

using OnlineExamSystem.Application.DTOs;

/// <summary>
/// Exam service interface for business logic
/// </summary>
public interface IExamService
{
    /// <summary>
    /// Get exam by ID
    /// </summary>
    Task<(bool Success, string Message, ExamResponse? Data)> GetExamByIdAsync(long id);

    /// <summary>
    /// Get all exams with pagination
    /// </summary>
    Task<(bool Success, string Message, ExamListResponse? Data)> GetAllExamsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Search exams by title or description
    /// </summary>
    Task<(bool Success, string Message, List<ExamResponse>? Data)> SearchExamsAsync(string searchTerm);

    /// <summary>
    /// Get exams created by a teacher
    /// </summary>
    Task<(bool Success, string Message, List<ExamResponse>? Data)> GetExamsByTeacherAsync(long teacherId);

    /// <summary>
    /// Get exams by subject
    /// </summary>
    Task<(bool Success, string Message, List<ExamResponse>? Data)> GetExamsBySubjectAsync(long subjectId);

    /// <summary>
    /// Create new exam
    /// </summary>
    Task<(bool Success, string Message, ExamResponse? Data)> CreateExamAsync(CreateExamRequest request);

    /// <summary>
    /// Update exam information
    /// </summary>
    Task<(bool Success, string Message, ExamResponse? Data)> UpdateExamAsync(long id, UpdateExamRequest request);

    /// <summary>
    /// Delete exam (only if in DRAFT status)
    /// </summary>
    Task<(bool Success, string Message)> DeleteExamAsync(long id);
}
