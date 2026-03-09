namespace OnlineExamSystem.Application.Repositories;

using OnlineExamSystem.Domain.Entities;

/// <summary>
/// Teacher repository interface for CRUD operations
/// </summary>
public interface ITeacherRepository
{
    /// <summary>
    /// Get teacher by ID with related user information
    /// </summary>
    Task<Teacher?> GetByIdAsync(long id);

    /// <summary>
    /// Get teacher by User ID
    /// </summary>
    Task<Teacher?> GetByUserIdAsync(long userId);

    /// <summary>
    /// Get teacher by Employee ID (unique)
    /// </summary>
    Task<Teacher?> GetByEmployeeIdAsync(string employeeId);

    /// <summary>
    /// Get all teachers with pagination
    /// </summary>
    Task<(List<Teacher> Teachers, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Search teachers by name or employee ID
    /// </summary>
    Task<List<Teacher>> SearchAsync(string searchTerm);

    /// <summary>
    /// Get classes assigned to a teacher
    /// </summary>
    Task<List<ClassTeacher>> GetTeacherClassesAsync(long teacherId);

    /// <summary>
    /// Create new teacher
    /// </summary>
    Task<Teacher> CreateAsync(Teacher teacher);

    /// <summary>
    /// Update teacher information
    /// </summary>
    Task<Teacher> UpdateAsync(Teacher teacher);

    /// <summary>
    /// Delete teacher
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// Check if employee ID exists
    /// </summary>
    Task<bool> EmployeeIdExistsAsync(string employeeId, long? excludeTeacherId = null);
}
