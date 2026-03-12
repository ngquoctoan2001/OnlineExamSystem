namespace OnlineExamSystem.Infrastructure.Services;

using OnlineExamSystem.Application.DTOs;

/// <summary>
/// Student service interface for business logic
/// </summary>
public interface IStudentService
{
    /// <summary>
    /// Get student by ID
    /// </summary>
    Task<(bool Success, string Message, StudentResponse? Data)> GetStudentByIdAsync(long id);

    /// <summary>
    /// Get student by User ID (for current authenticated user)
    /// </summary>
    Task<(bool Success, string Message, StudentResponse? Data)> GetStudentByUserIdAsync(long userId);

    /// <summary>
    /// Get all students with pagination
    /// </summary>
    Task<(bool Success, string Message, StudentListResponse? Data)> GetAllStudentsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Search students by name or student code
    /// </summary>
    Task<(bool Success, string Message, List<StudentResponse>? Data)> SearchStudentsAsync(string searchTerm);

    /// <summary>
    /// Create new student with user account
    /// </summary>
    Task<(bool Success, string Message, StudentResponse? Data)> CreateStudentAsync(CreateStudentRequest request);

    /// <summary>
    /// Update student information
    /// </summary>
    Task<(bool Success, string Message, StudentResponse? Data)> UpdateStudentAsync(long id, UpdateStudentRequest request);

    /// <summary>
    /// Delete student
    /// </summary>
    Task<(bool Success, string Message)> DeleteStudentAsync(long id);

    /// <summary>
    /// Get classes enrolled by student
    /// </summary>
    Task<(bool Success, string Message, List<StudentClassEnrollmentResponse>? Data)> GetStudentClassesAsync(long studentId);
}
