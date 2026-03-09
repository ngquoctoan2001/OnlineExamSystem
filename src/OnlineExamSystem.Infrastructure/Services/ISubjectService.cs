namespace OnlineExamSystem.Infrastructure.Services;

using OnlineExamSystem.Application.DTOs;

/// <summary>
/// Subject service interface for business logic
/// </summary>
public interface ISubjectService
{
    /// <summary>
    /// Get subject by ID
    /// </summary>
    Task<(bool Success, string Message, SubjectResponse? Data)> GetSubjectByIdAsync(long id);

    /// <summary>
    /// Get all subjects with pagination
    /// </summary>
    Task<(bool Success, string Message, SubjectListResponse? Data)> GetAllSubjectsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Search subjects by name or code
    /// </summary>
    Task<(bool Success, string Message, List<SubjectResponse>? Data)> SearchSubjectsAsync(string searchTerm);

    /// <summary>
    /// Create new subject
    /// </summary>
    Task<(bool Success, string Message, SubjectResponse? Data)> CreateSubjectAsync(CreateSubjectRequest request);

    /// <summary>
    /// Update subject information
    /// </summary>
    Task<(bool Success, string Message, SubjectResponse? Data)> UpdateSubjectAsync(long id, UpdateSubjectRequest request);

    /// <summary>
    /// Delete subject
    /// </summary>
    Task<(bool Success, string Message)> DeleteSubjectAsync(long id);
}
