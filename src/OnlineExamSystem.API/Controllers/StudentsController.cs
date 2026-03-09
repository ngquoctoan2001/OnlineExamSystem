namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Student management API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all students
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseResult<StudentListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting all students: page={Page}, pageSize={PageSize}", page, pageSize);
        
        var (success, message, data) = await _studentService.GetAllStudentsAsync(page, pageSize);
        
        return Ok(new ResponseResult<StudentListResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get student by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<StudentResponse>>> GetById(long id)
    {
        _logger.LogInformation("Getting student: {StudentId}", id);
        
        var (success, message, data) = await _studentService.GetStudentByIdAsync(id);
        
        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<StudentResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Search students by name or student code
    /// </summary>
    [HttpGet("search/{searchTerm}")]
    public async Task<ActionResult<ResponseResult<List<StudentResponse>>>> Search(string searchTerm)
    {
        _logger.LogInformation("Searching students: {SearchTerm}", searchTerm);
        
        var (success, message, data) = await _studentService.SearchStudentsAsync(searchTerm);
        
        return Ok(new ResponseResult<List<StudentResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Create new student
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResponseResult<StudentResponse>>> Create([FromBody] CreateStudentRequest request)
    {
        _logger.LogInformation("Creating new student: {Username}", request.Username);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _studentService.CreateStudentAsync(request);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = data?.Id }, new ResponseResult<StudentResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Update student information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseResult<StudentResponse>>> Update(long id, [FromBody] UpdateStudentRequest request)
    {
        _logger.LogInformation("Updating student: {StudentId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _studentService.UpdateStudentAsync(id, request);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<StudentResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Delete student
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        _logger.LogInformation("Deleting student: {StudentId}", id);

        var (success, message) = await _studentService.DeleteStudentAsync(id);

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
    /// Get classes enrolled by student
    /// </summary>
    [HttpGet("{id}/classes")]
    public async Task<ActionResult<ResponseResult<List<StudentClassEnrollmentResponse>>>> GetStudentClasses(long id)
    {
        _logger.LogInformation("Getting classes for student: {StudentId}", id);

        var (success, message, data) = await _studentService.GetStudentClassesAsync(id);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<List<StudentClassEnrollmentResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }
}
