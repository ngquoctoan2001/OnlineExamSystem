namespace OnlineExamSystem.Application.Services;

using OnlineExamSystem.Application.DTOs;

/// <summary>
/// Teacher service interface for business logic
/// </summary>
public interface ITeacherService
{
    /// <summary>
    /// Get teacher by ID
    /// </summary>
    Task<(bool Success, string Message, TeacherResponse? Data)> GetTeacherByIdAsync(long id);

    /// <summary>
    /// Get all teachers with pagination
    /// </summary>
    Task<(bool Success, string Message, TeacherListResponse? Data)> GetAllTeachersAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Search teachers by name or employee ID
    /// </summary>
    Task<(bool Success, string Message, List<TeacherResponse>? Data)> SearchTeachersAsync(string searchTerm);

    /// <summary>
    /// Create new teacher with user account
    /// </summary>
    Task<(bool Success, string Message, TeacherResponse? Data)> CreateTeacherAsync(CreateTeacherRequest request);

    /// <summary>
    /// Update teacher information
    /// </summary>
    Task<(bool Success, string Message, TeacherResponse? Data)> UpdateTeacherAsync(long id, UpdateTeacherRequest request);

    /// <summary>
    /// Delete teacher
    /// </summary>
    Task<(bool Success, string Message)> DeleteTeacherAsync(long id);

    /// <summary>
    /// Get classes assigned to teacher
    /// </summary>
    Task<(bool Success, string Message, List<TeacherClassAssignmentResponse>? Data)> GetTeacherClassesAsync(long teacherId);
}
