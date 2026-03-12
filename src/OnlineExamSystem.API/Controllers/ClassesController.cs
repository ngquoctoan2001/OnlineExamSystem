namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Data;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Class management API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Classes")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;
    private readonly ITeachingAssignmentService _teachingAssignmentService;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ClassesController> _logger;

    public ClassesController(
        IClassService classService,
        ITeachingAssignmentService teachingAssignmentService,
        ITeacherRepository teacherRepository,
        ApplicationDbContext dbContext,
        ILogger<ClassesController> logger)
    {
        _classService = classService;
        _teachingAssignmentService = teachingAssignmentService;
        _teacherRepository = teacherRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all classes
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<ClassListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting all classes: page={Page}, pageSize={PageSize}", page, pageSize);

        if (User.IsInRole("TEACHER") && !User.IsInRole("ADMIN"))
        {
            var userIdValue = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new ResponseResult<object>
                {
                    Success = false,
                    Message = "Không thể xác định người dùng"
                });
            }

            var teacher = await _teacherRepository.GetByUserIdAsync(userId);
            if (teacher == null)
            {
                return Ok(new ResponseResult<ClassListResponse>
                {
                    Success = true,
                    Message = "Không có lớp được phân công",
                    Data = new ClassListResponse
                    {
                        TotalCount = 0,
                        PageSize = pageSize,
                        CurrentPage = page,
                        TotalPages = 0,
                        Classes = new List<ClassResponse>()
                    }
                });
            }

            var (aSuccess, aMessage, assignments) = await _teachingAssignmentService.GetAssignmentsByTeacherAsync(teacher.Id);
            if (!aSuccess)
            {
                return BadRequest(new ResponseResult<object>
                {
                    Success = false,
                    Message = aMessage
                });
            }

            var classIds = assignments?
                .Select(a => a.ClassId)
                .Distinct()
                .ToList() ?? new List<long>();

            var classList = await _dbContext.Classes
                .Include(c => c.ClassStudents)
                .Include(c => c.ClassTeachers)
                .Include(c => c.HomeroomTeacher)
                    .ThenInclude(t => t!.User)
                .Where(c => classIds.Contains(c.Id))
                .OrderByDescending(c => c.Grade)
                .ThenBy(c => c.Name)
                .Select(c => new ClassResponse
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Grade = c.Grade,
                    HomeroomTeacherId = c.HomeroomTeacherId,
                    HomeroomTeacherName = c.HomeroomTeacher != null ? c.HomeroomTeacher.User.FullName : null,
                    StudentCount = c.ClassStudents.Count,
                    TeacherCount = c.ClassTeachers.Count
                })
                .ToListAsync();

            var totalCount = classList.Count;
            var paged = classList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Ok(new ResponseResult<ClassListResponse>
            {
                Success = true,
                Message = "Success",
                Data = new ClassListResponse
                {
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    CurrentPage = page,
                    TotalPages = totalCount == 0 ? 0 : (totalCount + pageSize - 1) / pageSize,
                    Classes = paged
                }
            });
        }
        
        var (success, message, data) = await _classService.GetAllClassesAsync(page, pageSize);
        
        return Ok(new ResponseResult<ClassListResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get class by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<ClassResponse>>> GetById(long id)
    {
        _logger.LogInformation("Getting class: {ClassId}", id);
        
        var (success, message, data) = await _classService.GetClassByIdAsync(id);
        
        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<ClassResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get classes by school
    /// </summary>
    [HttpGet("school/{schoolId}")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<ClassListResponse>>> GetBySchool(long schoolId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting classes for school: {SchoolId}", schoolId);
        
        var (success, message, data) = await _classService.GetClassesBySchoolAsync(schoolId, page, pageSize);
        
        return Ok(new ResponseResult<ClassListResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get classes by grade
    /// </summary>
    [HttpGet("grade/{grade}")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<ClassListResponse>>> GetByGrade(int grade, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting classes for grade: {Grade}", grade);
        
        var (success, message, data) = await _classService.GetClassesByGradeAsync(grade, page, pageSize);
        
        return Ok(new ResponseResult<ClassListResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Search classes by name or code
    /// </summary>
    [HttpGet("search/{searchTerm}")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<List<ClassResponse>>>> Search(string searchTerm)
    {
        _logger.LogInformation("Searching classes: {SearchTerm}", searchTerm);
        
        var (success, message, data) = await _classService.SearchClassesAsync(searchTerm);
        
        return Ok(new ResponseResult<List<ClassResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Create new class
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<ClassResponse>>> Create([FromBody] CreateClassRequest request)
    {
        _logger.LogInformation("Creating new class: {Code}", request.Code);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _classService.CreateClassAsync(request);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = data?.Id }, new ResponseResult<ClassResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Update class information
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<ClassResponse>>> Update(long id, [FromBody] UpdateClassRequest request)
    {
        _logger.LogInformation("Updating class: {ClassId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _classService.UpdateClassAsync(id, request);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<ClassResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Delete class
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        _logger.LogInformation("Deleting class: {ClassId}", id);

        var (success, message) = await _classService.DeleteClassAsync(id);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<object>
        {
            Success = success,
            Message = message
        });
    }

    /// <summary>
    /// Get students in class
    /// </summary>
    [HttpGet("{id}/students")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<List<ClassStudentResponse>>>> GetClassStudents(long id)
    {
        _logger.LogInformation("Getting students for class: {ClassId}", id);

        var (success, message, data) = await _classService.GetClassStudentsAsync(id);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<List<ClassStudentResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get teachers assigned to a class
    /// </summary>
    [HttpGet("{id}/teachers")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<List<TeacherAssignmentResponse>>>> GetClassTeachers(long id)
    {
        _logger.LogInformation("Getting teachers for class: {ClassId}", id);

        var (success, message, data) = await _teachingAssignmentService.GetAssignmentsByClassAsync(id);
        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<List<TeacherAssignmentResponse>>
        {
            Success = true,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Add student to class
    /// </summary>
    [HttpPost("{classId}/students/{studentId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<object>>> AddStudent(long classId, long studentId)
    {
        _logger.LogInformation("Adding student {StudentId} to class {ClassId}", studentId, classId);

        var (success, message) = await _classService.AddStudentToClassAsync(classId, studentId);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<object>
        {
            Success = success,
            Message = message
        });
    }

    /// <summary>
    /// Remove student from class
    /// </summary>
    [HttpDelete("{classId}/students/{studentId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<object>>> RemoveStudent(long classId, long studentId)
    {
        _logger.LogInformation("Removing student {StudentId} from class {ClassId}", studentId, classId);

        var (success, message) = await _classService.RemoveStudentFromClassAsync(classId, studentId);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<object>
        {
            Success = success,
            Message = message
        });
    }

    /// <summary>
    /// Assign teacher to class
    /// </summary>
    [HttpPost("{classId}/assign-teacher")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<object>>> AssignTeacher(long classId, [FromBody] AssignTeacherToClassRequest request)
    {
        _logger.LogInformation("Assigning teacher {TeacherId} to class {ClassId}", request.TeacherId, classId);

        var (success, message, _) = await _teachingAssignmentService.CreateAssignmentAsync(
            new CreateTeachingAssignmentRequest
            {
                ClassId = classId,
                TeacherId = request.TeacherId,
                SubjectId = request.SubjectId
            });

        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    /// <summary>
    /// Assign multiple students to class
    /// </summary>
    [HttpPost("{classId}/assign-students")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<object>>> AssignStudents(long classId, [FromBody] AssignStudentsToClassRequest request)
    {
        _logger.LogInformation("Assigning {Count} students to class {ClassId}", request.StudentIds.Count, classId);

        var errors = new List<string>();
        var successCount = 0;

        foreach (var studentId in request.StudentIds)
        {
            var (success, message) = await _classService.AddStudentToClassAsync(classId, studentId);
            if (success)
                successCount++;
            else
                errors.Add($"Student {studentId}: {message}");
        }

        return Ok(new ResponseResult<object>
        {
            Success = errors.Count == 0,
            Message = $"Assigned {successCount}/{request.StudentIds.Count} students",
            Errors = errors.Count > 0 ? errors : null
        });
    }
}
