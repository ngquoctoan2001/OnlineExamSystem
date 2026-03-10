namespace OnlineExamSystem.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

/// <summary>
/// Class service implementation with business logic
/// </summary>
public class ClassService : IClassService
{
    private readonly IClassRepository _classRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<ClassService> _logger;

    public ClassService(
        IClassRepository classRepository,
        IStudentRepository studentRepository,
        ILogger<ClassService> logger)
    {
        _classRepository = classRepository;
        _studentRepository = studentRepository;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ClassResponse? Data)> GetClassByIdAsync(long id)
    {
        try
        {
            var @class = await _classRepository.GetByIdAsync(id);
            
            if (@class == null)
            {
                _logger.LogWarning("Class not found: {ClassId}", id);
                return (false, "Class not found", null);
            }

            var response = MapToClassResponse(@class);
            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting class {ClassId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ClassListResponse? Data)> GetAllClassesAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1 || pageSize < 1)
            {
                return (false, "Page and pageSize must be greater than 0", null);
            }

            var (classes, totalCount) = await _classRepository.GetAllAsync(page, pageSize);
            
            var classResponses = classes.Select(MapToClassResponse).ToList();
            
            var response = new ClassListResponse
            {
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (totalCount + pageSize - 1) / pageSize,
                Classes = classResponses
            };

            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all classes");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ClassListResponse? Data)> GetClassesBySchoolAsync(long schoolId, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1 || pageSize < 1)
            {
                return (false, "Page and pageSize must be greater than 0", null);
            }

            var (classes, totalCount) = await _classRepository.GetBySchoolAsync(schoolId, page, pageSize);
            
            var classResponses = classes.Select(MapToClassResponse).ToList();
            
            var response = new ClassListResponse
            {
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (totalCount + pageSize - 1) / pageSize,
                Classes = classResponses
            };

            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting classes by school {SchoolId}", schoolId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ClassListResponse? Data)> GetClassesByGradeAsync(int grade, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1 || pageSize < 1)
            {
                return (false, "Page and pageSize must be greater than 0", null);
            }

            var (classes, totalCount) = await _classRepository.GetByGradeAsync(grade, page, pageSize);
            
            var classResponses = classes.Select(MapToClassResponse).ToList();
            
            var response = new ClassListResponse
            {
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (totalCount + pageSize - 1) / pageSize,
                Classes = classResponses
            };

            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting classes by grade {Grade}", grade);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ClassResponse>? Data)> SearchClassesAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return (false, "Search term cannot be empty", null);
            }

            var classes = await _classRepository.SearchAsync(searchTerm.Trim());
            var responses = classes.Select(MapToClassResponse).ToList();

            return (true, $"Found {responses.Count} class(es)", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching classes with term: {SearchTerm}", searchTerm);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ClassResponse? Data)> CreateClassAsync(CreateClassRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Code) || 
                string.IsNullOrWhiteSpace(request.Name) ||
                request.Grade < 1 || request.Grade > 12)
            {
                return (false, "Invalid request data", null);
            }

            // Check if code exists
            var codeExists = await _classRepository.CodeExistsAsync(request.Code);
            if (codeExists)
            {
                _logger.LogWarning("Class code already exists: {Code}", request.Code);
                return (false, "Class code already exists", null);
            }

            // Create class - default SchoolId to 1
            var @class = new Class
            {
                SchoolId = 1,
                Code = request.Code,
                Name = request.Name,
                Grade = request.Grade,
                HomeroomTeacherId = request.HomeroomTeacherId
            };

            var createdClass = await _classRepository.CreateAsync(@class);

            _logger.LogInformation("Class created successfully: {Code}", request.Code);
            
            var response = MapToClassResponse(createdClass);
            return (true, "Class created successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating class");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ClassResponse? Data)> UpdateClassAsync(long id, UpdateClassRequest request)
    {
        try
        {
            var @class = await _classRepository.GetByIdAsync(id);
            if (@class == null)
            {
                _logger.LogWarning("Class not found: {ClassId}", id);
                return (false, "Class not found", null);
            }

            // Update information
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                @class.Name = request.Name;
            }

            if (request.Grade.HasValue && request.Grade > 0 && request.Grade <= 12)
            {
                @class.Grade = request.Grade.Value;
            }

            if (request.HomeroomTeacherId.HasValue)
            {
                @class.HomeroomTeacherId = request.HomeroomTeacherId.Value == 0 ? null : request.HomeroomTeacherId;
            }

            var updatedClass = await _classRepository.UpdateAsync(@class);
            var response = MapToClassResponse(updatedClass);

            _logger.LogInformation("Class updated: {ClassId}", id);
            return (true, "Class updated successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating class {ClassId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteClassAsync(long id)
    {
        try
        {
            var @class = await _classRepository.GetByIdAsync(id);
            if (@class == null)
            {
                _logger.LogWarning("Class not found: {ClassId}", id);
                return (false, "Class not found");
            }

            var result = await _classRepository.DeleteAsync(id);
            if (!result)
            {
                return (false, "Failed to delete class");
            }

            _logger.LogInformation("Class deleted: {ClassId}", id);
            return (true, "Class deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting class {ClassId}", id);
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, List<ClassStudentResponse>? Data)> GetClassStudentsAsync(long classId)
    {
        try
        {
            var @class = await _classRepository.GetByIdAsync(classId);
            if (@class == null)
            {
                _logger.LogWarning("Class not found: {ClassId}", classId);
                return (false, "Class not found", null);
            }

            var classStudents = await _classRepository.GetClassStudentsAsync(classId);
            
            var responses = classStudents.Select(cs => new ClassStudentResponse
            {
                StudentId = cs.StudentId,
                Username = cs.Student?.User?.Username ?? string.Empty,
                FullName = cs.Student?.User?.FullName ?? string.Empty,
                StudentCode = cs.Student?.StudentCode ?? string.Empty,
                RollNumber = cs.Student?.RollNumber ?? string.Empty,
                EnrolledAt = cs.EnrolledAt
            }).ToList();

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students for class {ClassId}", classId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> AddStudentToClassAsync(long classId, long studentId)
    {
        try
        {
            var @class = await _classRepository.GetByIdAsync(classId);
            if (@class == null)
            {
                return (false, "Class not found");
            }

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null)
            {
                return (false, "Student not found");
            }

            // Check if already enrolled
            var alreadyEnrolled = await _classRepository.StudentEnrolledInClassAsync(classId, studentId);
            if (alreadyEnrolled)
            {
                return (false, "Student is already enrolled in this class");
            }

            var result = await _classRepository.AddStudentToClassAsync(classId, studentId);
            if (!result)
            {
                return (false, "Failed to add student to class");
            }

            _logger.LogInformation("Student {StudentId} added to class {ClassId}", studentId, classId);
            return (true, "Student added to class successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding student {StudentId} to class {ClassId}", studentId, classId);
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RemoveStudentFromClassAsync(long classId, long studentId)
    {
        try
        {
            var @class = await _classRepository.GetByIdAsync(classId);
            if (@class == null)
            {
                return (false, "Class not found");
            }

            var result = await _classRepository.RemoveStudentFromClassAsync(classId, studentId);
            if (!result)
            {
                return (false, "Student is not enrolled in this class");
            }

            _logger.LogInformation("Student {StudentId} removed from class {ClassId}", studentId, classId);
            return (true, "Student removed from class successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing student {StudentId} from class {ClassId}", studentId, classId);
            return (false, $"Error: {ex.Message}");
        }
    }

    private ClassResponse MapToClassResponse(Class @class)
    {
        return new ClassResponse
        {
            Id = @class.Id,
            Code = @class.Code,
            Name = @class.Name,
            Grade = @class.Grade,
            HomeroomTeacherId = @class.HomeroomTeacherId,
            HomeroomTeacherName = @class.HomeroomTeacher?.User?.FullName,
            StudentCount = @class.ClassStudents?.Count ?? 0,
            TeacherCount = @class.ClassTeachers?.Count ?? 0
        };
    }
}
