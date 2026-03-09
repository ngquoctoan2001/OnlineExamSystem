namespace OnlineExamSystem.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

/// <summary>
/// Student service implementation with business logic
/// </summary>
public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<StudentService> _logger;

    public StudentService(
        IStudentRepository studentRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<StudentService> logger)
    {
        _studentRepository = studentRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, StudentResponse? Data)> GetStudentByIdAsync(long id)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(id);
            
            if (student == null)
            {
                _logger.LogWarning("Student not found: {StudentId}", id);
                return (false, "Student not found", null);
            }

            var response = MapToStudentResponse(student);
            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student {StudentId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, StudentListResponse? Data)> GetAllStudentsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1 || pageSize < 1)
            {
                return (false, "Page and pageSize must be greater than 0", null);
            }

            var (students, totalCount) = await _studentRepository.GetAllAsync(page, pageSize);
            
            var studentResponses = students.Select(MapToStudentResponse).ToList();
            
            var response = new StudentListResponse
            {
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page,
                TotalPages = (totalCount + pageSize - 1) / pageSize,
                Students = studentResponses
            };

            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all students");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<StudentResponse>? Data)> SearchStudentsAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return (false, "Search term cannot be empty", null);
            }

            var students = await _studentRepository.SearchAsync(searchTerm.Trim());
            var responses = students.Select(MapToStudentResponse).ToList();

            return (true, $"Found {responses.Count} student(s)", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching students with term: {SearchTerm}", searchTerm);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, StudentResponse? Data)> CreateStudentAsync(CreateStudentRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.StudentCode))
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

            // Check if student code exists
            var studentCodeExists = await _studentRepository.StudentCodeExistsAsync(request.StudentCode);
            if (studentCodeExists)
            {
                _logger.LogWarning("Student code already exists: {StudentCode}", request.StudentCode);
                return (false, "Student code already exists", null);
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

            // Create student record
            var student = new Student
            {
                UserId = createdUser.Id,
                StudentCode = request.StudentCode,
                RollNumber = request.RollNumber
            };

            var createdStudent = await _studentRepository.CreateAsync(student);
            createdStudent.User = createdUser;

            _logger.LogInformation("Student created successfully: {StudentCode}", request.StudentCode);
            
            var response = MapToStudentResponse(createdStudent);
            return (true, "Student created successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, StudentResponse? Data)> UpdateStudentAsync(long id, UpdateStudentRequest request)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
            {
                _logger.LogWarning("Student not found: {StudentId}", id);
                return (false, "Student not found", null);
            }

            // Update user information
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                student.User!.FullName = request.FullName;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Check if email is already used by another user
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != student.UserId)
                {
                    return (false, "Email already exists", null);
                }

                student.User.Email = request.Email;
            }

            if (!string.IsNullOrWhiteSpace(request.RollNumber))
            {
                student.RollNumber = request.RollNumber;
            }

            if (request.IsActive.HasValue)
            {
                student.User.IsActive = request.IsActive.Value;
            }

            student.User.UpdatedAt = DateTime.UtcNow;

            var updatedStudent = await _studentRepository.UpdateAsync(student);
            var response = MapToStudentResponse(updatedStudent);

            _logger.LogInformation("Student updated: {StudentId}", id);
            return (true, "Student updated successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student {StudentId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteStudentAsync(long id)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
            {
                _logger.LogWarning("Student not found: {StudentId}", id);
                return (false, "Student not found");
            }

            var result = await _studentRepository.DeleteAsync(id);
            if (!result)
            {
                return (false, "Failed to delete student");
            }

            _logger.LogInformation("Student deleted: {StudentId}", id);
            return (true, "Student deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student {StudentId}", id);
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, List<StudentClassEnrollmentResponse>? Data)> GetStudentClassesAsync(long studentId)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null)
            {
                _logger.LogWarning("Student not found: {StudentId}", studentId);
                return (false, "Student not found", null);
            }

            var classEnrollments = await _studentRepository.GetStudentClassesAsync(studentId);
            
            var responses = classEnrollments.Select(ce => new StudentClassEnrollmentResponse
            {
                Id = ce.ClassId,
                StudentId = ce.StudentId,
                ClassId = ce.ClassId,
                ClassName = ce.Class?.Name ?? string.Empty,
                ClassCode = ce.Class?.Code ?? string.Empty,
                EnrolledAt = ce.EnrolledAt
            }).ToList();

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student classes for student {StudentId}", studentId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    private StudentResponse MapToStudentResponse(Student student)
    {
        return new StudentResponse
        {
            Id = student.Id,
            UserId = student.UserId,
            Username = student.User?.Username ?? string.Empty,
            Email = student.User?.Email ?? string.Empty,
            FullName = student.User?.FullName ?? string.Empty,
            StudentCode = student.StudentCode,
            RollNumber = student.RollNumber,
            IsActive = student.User?.IsActive ?? false,
            CreatedAt = student.User?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = student.User?.UpdatedAt ?? DateTime.UtcNow
        };
    }
}
