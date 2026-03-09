namespace OnlineExamSystem.Infrastructure.Services;

using OnlineExamSystem.Application.DTOs;

/// <summary>
/// Class service interface for business logic
/// </summary>
public interface IClassService
{
    /// <summary>
    /// Get class by ID
    /// </summary>
    Task<(bool Success, string Message, ClassResponse? Data)> GetClassByIdAsync(long id);

    /// <summary>
    /// Get all classes with pagination
    /// </summary>
    Task<(bool Success, string Message, ClassListResponse? Data)> GetAllClassesAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Get classes by school
    /// </summary>
    Task<(bool Success, string Message, ClassListResponse? Data)> GetClassesBySchoolAsync(long schoolId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get classes by grade level
    /// </summary>
    Task<(bool Success, string Message, ClassListResponse? Data)> GetClassesByGradeAsync(int grade, int page = 1, int pageSize = 20);

    /// <summary>
    /// Search classes by name or code
    /// </summary>
    Task<(bool Success, string Message, List<ClassResponse>? Data)> SearchClassesAsync(string searchTerm);

    /// <summary>
    /// Create new class
    /// </summary>
    Task<(bool Success, string Message, ClassResponse? Data)> CreateClassAsync(CreateClassRequest request);

    /// <summary>
    /// Update class information
    /// </summary>
    Task<(bool Success, string Message, ClassResponse? Data)> UpdateClassAsync(long id, UpdateClassRequest request);

    /// <summary>
    /// Delete class
    /// </summary>
    Task<(bool Success, string Message)> DeleteClassAsync(long id);

    /// <summary>
    /// Get students enrolled in class
    /// </summary>
    Task<(bool Success, string Message, List<ClassStudentResponse>? Data)> GetClassStudentsAsync(long classId);

    /// <summary>
    /// Add student to class
    /// </summary>
    Task<(bool Success, string Message)> AddStudentToClassAsync(long classId, long studentId);

    /// <summary>
    /// Remove student from class
    /// </summary>
    Task<(bool Success, string Message)> RemoveStudentFromClassAsync(long classId, long studentId);
}
