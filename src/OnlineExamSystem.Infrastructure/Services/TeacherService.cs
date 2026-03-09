namespace OnlineExamSystem.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

/// <summary>
/// Teacher service implementation with business logic
/// </summary>
public class TeacherService : ITeacherService
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<TeacherService> _logger;

    public TeacherService(
        ITeacherRepository teacherRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<TeacherService> logger)
    {
        _teacherRepository = teacherRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, TeacherResponse? Data)> GetTeacherByIdAsync(long id)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdAsync(id);
            
            if (teacher == null)
            {
                _logger.LogWarning("Teacher not found: {TeacherId}", id);
                return (false, "Teacher not found", null);
            }

            var response = MapToTeacherResponse(teacher);
            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teacher {TeacherId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, TeacherListResponse? Data)> GetAllTeachersAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1 || pageSize < 1)
            {
                return (false, "Page and pageSize must be greater than 0", null);
            }

            var (teachers, totalCount) = await _teacherRepository.GetAllAsync(page, pageSize);
            
            var teacherResponses = teachers.Select(MapToTeacherResponse).ToList();
            
            var response = new TeacherListResponse
            {
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (totalCount + pageSize - 1) / pageSize,
                Teachers = teacherResponses
            };

            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all teachers");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<TeacherResponse>? Data)> SearchTeachersAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return (false, "Search term cannot be empty", null);
            }

            var teachers = await _teacherRepository.SearchAsync(searchTerm.Trim());
            var responses = teachers.Select(MapToTeacherResponse).ToList();

            return (true, $"Found {responses.Count} teacher(s)", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching teachers with term: {SearchTerm}", searchTerm);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, TeacherResponse? Data)> CreateTeacherAsync(CreateTeacherRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.EmployeeId))
            {
                return (false, "All fields are required", null);
            }

            // Check if username exists
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                _logger.LogWarning("Username already exists: {Username}", request.Username);
                return (false, "Username already exists", null);
            }

            // Check if email exists
            existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email already exists: {Email}", request.Email);
                return (false, "Email already exists", null);
            }

            // Check if employee ID exists
            var employeeExists = await _teacherRepository.EmployeeIdExistsAsync(request.EmployeeId);
            if (employeeExists)
            {
                _logger.LogWarning("Employee ID already exists: {EmployeeId}", request.EmployeeId);
                return (false, "Employee ID already exists", null);
            }

            // Create user account
            var passwordHash = _passwordHasher.HashPassword(request.Password);
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.CreateAsync(user);

            // Create teacher record
            var teacher = new Teacher
            {
                UserId = createdUser.Id,
                EmployeeId = request.EmployeeId,
                Department = request.Department
            };

            var createdTeacher = await _teacherRepository.CreateAsync(teacher);
            createdTeacher.User = createdUser;

            _logger.LogInformation("Teacher created successfully: {EmployeeId}", request.EmployeeId);
            
            var response = MapToTeacherResponse(createdTeacher);
            return (true, "Teacher created successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating teacher");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, TeacherResponse? Data)> UpdateTeacherAsync(long id, UpdateTeacherRequest request)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdAsync(id);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher not found: {TeacherId}", id);
                return (false, "Teacher not found", null);
            }

            // Update user information
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                teacher.User!.FullName = request.FullName;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Check if email is already used by another user
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != teacher.UserId)
                {
                    return (false, "Email already exists", null);
                }

                teacher.User.Email = request.Email;
            }

            if (!string.IsNullOrWhiteSpace(request.Department))
            {
                teacher.Department = request.Department;
            }

            if (request.IsActive.HasValue)
            {
                teacher.User.IsActive = request.IsActive.Value;
            }

            teacher.User.UpdatedAt = DateTime.UtcNow;

            var updatedTeacher = await _teacherRepository.UpdateAsync(teacher);
            var response = MapToTeacherResponse(updatedTeacher);

            _logger.LogInformation("Teacher updated: {TeacherId}", id);
            return (true, "Teacher updated successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating teacher {TeacherId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteTeacherAsync(long id)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdAsync(id);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher not found: {TeacherId}", id);
                return (false, "Teacher not found");
            }

            var result = await _teacherRepository.DeleteAsync(id);
            if (!result)
            {
                return (false, "Failed to delete teacher");
            }

            // Optionally also delete the user account
            // await _userRepository.DeleteAsync(teacher.UserId);

            _logger.LogInformation("Teacher deleted: {TeacherId}", id);
            return (true, "Teacher deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting teacher {TeacherId}", id);
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, List<TeacherClassAssignmentResponse>? Data)> GetTeacherClassesAsync(long teacherId)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdAsync(teacherId);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher not found: {TeacherId}", teacherId);
                return (false, "Teacher not found", null);
            }

            var classAssignments = await _teacherRepository.GetTeacherClassesAsync(teacherId);
            
            var responses = classAssignments.Select(ct => new TeacherClassAssignmentResponse
            {
                Id = ct.ClassId,
                TeacherId = ct.TeacherId,
                ClassId = ct.ClassId,
                ClassName = ct.Class?.Name ?? string.Empty,
                ClassCode = ct.Class?.Code ?? string.Empty,
                SubjectId = ct.SubjectId,
                SubjectName = string.Empty, // Will be populated if Subject is included
                AssignedAt = ct.AssignedAt
            }).ToList();

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teacher classes for teacher {TeacherId}", teacherId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    private TeacherResponse MapToTeacherResponse(Teacher teacher)
    {
        return new TeacherResponse
        {
            Id = teacher.Id,
            UserId = teacher.UserId,
            Username = teacher.User?.Username ?? string.Empty,
            Email = teacher.User?.Email ?? string.Empty,
            FullName = teacher.User?.FullName ?? string.Empty,
            EmployeeId = teacher.EmployeeId,
            Department = teacher.Department,
            IsActive = teacher.User?.IsActive ?? false,
            CreatedAt = teacher.User?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = teacher.User?.UpdatedAt ?? DateTime.UtcNow
        };
    }
}
