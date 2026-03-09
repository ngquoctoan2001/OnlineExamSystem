namespace OnlineExamSystem.Infrastructure.Services;

using OnlineExamSystem.Application.DTOs;

/// <summary>
/// Service interface for teaching assignment management
/// </summary>
public interface ITeachingAssignmentService
{
    /// <summary>
    /// Get teaching assignment by ID
    /// </summary>
    Task<(bool Success, string Message, TeachingAssignmentResponse? Data)> GetAssignmentByIdAsync(long id);

    /// <summary>
    /// Get all teaching assignments
    /// </summary>
    Task<(bool Success, string Message, TeachingAssignmentListResponse? Data)> GetAllAssignmentsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Get assignments for a specific class
    /// </summary>
    Task<(bool Success, string Message, List<TeacherAssignmentResponse>? Data)> GetAssignmentsByClassAsync(long classId);

    /// <summary>
    /// Get assignments for a specific teacher
    /// </summary>
    Task<(bool Success, string Message, List<SubjectAssignmentResponse>? Data)> GetAssignmentsByTeacherAsync(long teacherId);

    /// <summary>
    /// Create new teaching assignment
    /// </summary>
    Task<(bool Success, string Message, TeachingAssignmentResponse? Data)> CreateAssignmentAsync(CreateTeachingAssignmentRequest request);

    /// <summary>
    /// Update teaching assignment
    /// </summary>
    Task<(bool Success, string Message, TeachingAssignmentResponse? Data)> UpdateAssignmentAsync(long id, UpdateTeachingAssignmentRequest request);

    /// <summary>
    /// Delete teaching assignment
    /// </summary>
    Task<(bool Success, string Message)> DeleteAssignmentAsync(long id);
}
