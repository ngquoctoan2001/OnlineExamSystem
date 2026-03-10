namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
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
    private readonly ILogger<ClassesController> _logger;

    public ClassesController(
        IClassService classService,
        ITeachingAssignmentService teachingAssignmentService,
        ILogger<ClassesController> logger)
    {
        _classService = classService;
        _teachingAssignmentService = teachingAssignmentService;
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
}
