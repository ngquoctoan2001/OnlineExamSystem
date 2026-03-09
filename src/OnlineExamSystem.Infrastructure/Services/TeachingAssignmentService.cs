namespace OnlineExamSystem.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

/// <summary>
/// Implements teaching assignment service with business logic
/// </summary>
public class TeachingAssignmentService : ITeachingAssignmentService
{
    private readonly ITeachingAssignmentRepository _repository;
    private readonly IClassRepository _classRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<TeachingAssignmentService> _logger;

    public TeachingAssignmentService(
        ITeachingAssignmentRepository repository,
        IClassRepository classRepository,
        ITeacherRepository teacherRepository,
        ISubjectRepository subjectRepository,
        ILogger<TeachingAssignmentService> logger)
    {
        _repository = repository;
        _classRepository = classRepository;
        _teacherRepository = teacherRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get teaching assignment by ID
    /// </summary>
    public async Task<(bool Success, string Message, TeachingAssignmentResponse? Data)> GetAssignmentByIdAsync(long id)
    {
        try
        {
            var assignment = await _repository.GetByIdAsync(id);
            
            if (assignment == null)
            {
                _logger.LogWarning("Teaching assignment not found: {Id}", id);
                return (false, "Không tìm thấy giao nhiệm vụ", null);
            }

            var response = MapToResponse(assignment);
            return (true, "Lấy thành công", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignment by ID: {Id}", id);
            return (false, "Lỗi khi lấy giao nhiệm vụ", null);
        }
    }

    /// <summary>
    /// Get all teaching assignments
    /// </summary>
    public async Task<(bool Success, string Message, TeachingAssignmentListResponse? Data)> GetAllAssignmentsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (assignments, totalCount) = await _repository.GetAllAsync(page, pageSize);

            var items = assignments.Select(MapToResponse).ToList();
            
            var response = new TeachingAssignmentListResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return (true, "Lấy danh sách thành công", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all assignments");
            return (false, "Lỗi khi lấy danh sách giao nhiệm vụ", null);
        }
    }

    /// <summary>
    /// Get assignments for a specific class
    /// </summary>
    public async Task<(bool Success, string Message, List<TeacherAssignmentResponse>? Data)> GetAssignmentsByClassAsync(long classId)
    {
        try
        {
            // Verify class exists
            var @class = await _classRepository.GetByIdAsync(classId);
            if (@class == null)
            {
                _logger.LogWarning("Class not found for assignments: {ClassId}", classId);
                return (false, "Không tìm thấy lớp học", null);
            }

            var assignments = await _repository.GetByClassAsync(classId);
            
            var responses = assignments.Select(a => new TeacherAssignmentResponse
            {
                Id = a.Id,
                TeacherId = a.TeacherId,
                TeacherName = a.Teacher?.User?.FullName ?? string.Empty,
                SubjectId = a.SubjectId,
                SubjectName = a.Subject?.Name ?? string.Empty,
                SubjectCode = a.Subject?.Code ?? string.Empty,
                AcademicYear = a.AcademicYear,
                Semester = a.Semester,
                AssignedDate = a.CreatedDate
            }).ToList();

            return (true, "Lấy danh sách thành công", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignments for class: {ClassId}", classId);
            return (false, "Lỗi khi lấy giao nhiệm vụ của lớp", null);
        }
    }

    /// <summary>
    /// Get assignments for a specific teacher
    /// </summary>
    public async Task<(bool Success, string Message, List<SubjectAssignmentResponse>? Data)> GetAssignmentsByTeacherAsync(long teacherId)
    {
        try
        {
            // Verify teacher exists
            var teacher = await _teacherRepository.GetByIdAsync(teacherId);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher not found for assignments: {TeacherId}", teacherId);
                return (false, "Không tìm thấy giáo viên", null);
            }

            var assignments = await _repository.GetByTeacherAsync(teacherId);
            
            var responses = assignments.Select(a => new SubjectAssignmentResponse
            {
                Id = a.Id,
                ClassId = a.ClassId,
                ClassName = a.Class?.Name ?? string.Empty,
                SubjectId = a.SubjectId,
                SubjectName = a.Subject?.Name ?? string.Empty,
                SubjectCode = a.Subject?.Code ?? string.Empty,
                AcademicYear = a.AcademicYear,
                Semester = a.Semester,
                AssignedDate = a.CreatedDate
            }).ToList();

            return (true, "Lấy danh sách thành công", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignments for teacher: {TeacherId}", teacherId);
            return (false, "Lỗi khi lấy giao nhiệm vụ của giáo viên", null);
        }
    }

    /// <summary>
    /// Create new teaching assignment
    /// </summary>
    public async Task<(bool Success, string Message, TeachingAssignmentResponse? Data)> CreateAssignmentAsync(CreateTeachingAssignmentRequest request)
    {
        try
        {
            // Validate class exists
            var @class = await _classRepository.GetByIdAsync(request.ClassId);
            if (@class == null)
            {
                _logger.LogWarning("Class not found for assignment creation: {ClassId}", request.ClassId);
                return (false, "Không tìm thấy lớp học", null);
            }

            // Validate teacher exists
            var teacher = await _teacherRepository.GetByIdAsync(request.TeacherId);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher not found for assignment creation: {TeacherId}", request.TeacherId);
                return (false, "Không tìm thấy giáo viên", null);
            }

            // Validate subject exists
            var subject = await _subjectRepository.GetByIdAsync(request.SubjectId);
            if (subject == null)
            {
                _logger.LogWarning("Subject not found for assignment creation: {SubjectId}", request.SubjectId);
                return (false, "Không tìm thấy môn học", null);
            }

            // Check for duplicate assignment
            var exists = await _repository.AssignmentExistsAsync(request.ClassId, request.TeacherId, request.SubjectId);
            if (exists)
            {
                _logger.LogWarning("Assignment already exists");
                return (false, "Giao nhiệm vụ này đã tồn tại", null);
            }

            var assignment = new ClassTeacher
            {
                ClassId = request.ClassId,
                TeacherId = request.TeacherId,
                SubjectId = request.SubjectId,
                AcademicYear = request.AcademicYear,
                Semester = request.Semester
            };

            var created = await _repository.CreateAsync(assignment);
            
            // Reload with related data
            var result = await _repository.GetByIdAsync(created.Id);
            if (result == null)
            {
                return (false, "Lỗi khi tạo giao nhiệm vụ", null);
            }

            var response = MapToResponse(result);
            _logger.LogInformation("Teaching assignment created successfully");
            return (true, "Tạo giao nhiệm vụ thành công", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment");
            return (false, "Lỗi khi tạo giao nhiệm vụ", null);
        }
    }

    /// <summary>
    /// Update teaching assignment
    /// </summary>
    public async Task<(bool Success, string Message, TeachingAssignmentResponse? Data)> UpdateAssignmentAsync(long id, UpdateTeachingAssignmentRequest request)
    {
        try
        {
            var assignment = await _repository.GetByIdAsync(id);
            if (assignment == null)
            {
                _logger.LogWarning("Assignment not found for update: {Id}", id);
                return (false, "Không tìm thấy giao nhiệm vụ", null);
            }

            // Validate subject exists
            if (assignment.SubjectId != request.SubjectId)
            {
                var subject = await _subjectRepository.GetByIdAsync(request.SubjectId);
                if (subject == null)
                {
                    _logger.LogWarning("Subject not found: {SubjectId}", request.SubjectId);
                    return (false, "Không tìm thấy môn học", null);
                }
            }

            // Check for duplicate assignment with new values
            var exists = await _repository.AssignmentExistsAsync(
                assignment.ClassId, 
                assignment.TeacherId, 
                request.SubjectId, 
                id);
            if (exists)
            {
                return (false, "Giao nhiệm vụ này đã tồn tại", null);
            }

            assignment.SubjectId = request.SubjectId;
            assignment.AcademicYear = request.AcademicYear;
            assignment.Semester = request.Semester;

            var updated = await _repository.UpdateAsync(assignment);
            
            // Reload with related data
            var result = await _repository.GetByIdAsync(updated.Id);
            if (result == null)
            {
                return (false, "Lỗi khi cập nhật giao nhiệm vụ", null);
            }

            var response = MapToResponse(result);
            _logger.LogInformation("Teaching assignment updated successfully");
            return (true, "Cập nhật giao nhiệm vụ thành công", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment: {Id}", id);
            return (false, "Lỗi khi cập nhật giao nhiệm vụ", null);
        }
    }

    /// <summary>
    /// Delete teaching assignment
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAssignmentAsync(long id)
    {
        try
        {
            var assignment = await _repository.GetByIdAsync(id);
            if (assignment == null)
            {
                _logger.LogWarning("Assignment not found for deletion: {Id}", id);
                return (false, "Không tìm thấy giao nhiệm vụ");
            }

            var success = await _repository.DeleteAsync(id);
            if (!success)
            {
                return (false, "Lỗi khi xóa giao nhiệm vụ");
            }

            _logger.LogInformation("Teaching assignment deleted successfully");
            return (true, "Xóa giao nhiệm vụ thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment: {Id}", id);
            return (false, "Lỗi khi xóa giao nhiệm vụ");
        }
    }

    /// <summary>
    /// Map ClassTeacher entity to TeachingAssignmentResponse DTO
    /// </summary>
    private static TeachingAssignmentResponse MapToResponse(ClassTeacher assignment)
    {
        return new TeachingAssignmentResponse
        {
            Id = assignment.Id,
            ClassId = assignment.ClassId,
            ClassName = assignment.Class?.Name ?? string.Empty,
            TeacherId = assignment.TeacherId,
            TeacherName = assignment.Teacher?.User?.FullName ?? string.Empty,
            SubjectId = assignment.SubjectId,
            SubjectName = assignment.Subject?.Name ?? string.Empty,
            SubjectCode = assignment.Subject?.Code ?? string.Empty,
            AcademicYear = assignment.AcademicYear,
            Semester = assignment.Semester,
            AssignedDate = assignment.CreatedDate
        };
    }
}
