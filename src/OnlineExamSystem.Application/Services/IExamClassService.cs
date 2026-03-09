namespace OnlineExamSystem.Infrastructure.Services;

using OnlineExamSystem.Application.DTOs;

/// <summary>
/// Exam-Class assignment service interface
/// </summary>
public interface IExamClassService
{
    /// <summary>
    /// Assign a class to an exam
    /// </summary>
    Task<(bool Success, string Message, ExamClassResponse? Data)> AssignClassToExamAsync(long examId, long classId);

    /// <summary>
    /// Get all classes assigned to an exam
    /// </summary>
    Task<(bool Success, string Message, ExamClassesResponse? Data)> GetExamClassesAsync(long examId);

    /// <summary>
    /// Get all exams assigned to a class
    /// </summary>
    Task<(bool Success, string Message, List<ExamClassResponse>? Data)> GetClassExamsAsync(long classId);

    /// <summary>
    /// Remove a class from an exam
    /// </summary>
    Task<(bool Success, string Message)> RemoveClassFromExamAsync(long examId, long classId);

    /// <summary>
    /// Get paginated list of all exam-class assignments
    /// </summary>
    Task<(bool Success, string Message, ExamClassListResponse? Data)> GetAllAssignmentsAsync(int page = 1, int pageSize = 20);
}
