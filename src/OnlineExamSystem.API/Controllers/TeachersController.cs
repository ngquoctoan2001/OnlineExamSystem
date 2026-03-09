namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Teacher management API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeachersController : ControllerBase
{
    private readonly ITeacherService _teacherService;
    private readonly ILogger<TeachersController> _logger;

    public TeachersController(ITeacherService teacherService, ILogger<TeachersController> logger)
    {
        _teacherService = teacherService;
        _logger = logger;
    }

    /// <summary>
    /// Get all teachers
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseResult<TeacherListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting all teachers: page={Page}, pageSize={PageSize}", page, pageSize);
        
        var (success, message, data) = await _teacherService.GetAllTeachersAsync(page, pageSize);
        
        return Ok(new ResponseResult<TeacherListResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get teacher by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<TeacherResponse>>> GetById(long id)
    {
        _logger.LogInformation("Getting teacher: {TeacherId}", id);
        
        var (success, message, data) = await _teacherService.GetTeacherByIdAsync(id);
        
        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<TeacherResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Search teachers by name or employee ID
    /// </summary>
    [HttpGet("search/{searchTerm}")]
    public async Task<ActionResult<ResponseResult<List<TeacherResponse>>>> Search(string searchTerm)
    {
        _logger.LogInformation("Searching teachers: {SearchTerm}", searchTerm);
        
        var (success, message, data) = await _teacherService.SearchTeachersAsync(searchTerm);
        
        return Ok(new ResponseResult<List<TeacherResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Create new teacher
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResponseResult<TeacherResponse>>> Create([FromBody] CreateTeacherRequest request)
    {
        _logger.LogInformation("Creating new teacher: {Username}", request.Username);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _teacherService.CreateTeacherAsync(request);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = data?.Id }, new ResponseResult<TeacherResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Update teacher information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseResult<TeacherResponse>>> Update(long id, [FromBody] UpdateTeacherRequest request)
    {
        _logger.LogInformation("Updating teacher: {TeacherId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _teacherService.UpdateTeacherAsync(id, request);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<TeacherResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Delete teacher
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        _logger.LogInformation("Deleting teacher: {TeacherId}", id);

        var (success, message) = await _teacherService.DeleteTeacherAsync(id);

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
    /// Get classes assigned to a teacher
    /// </summary>
    [HttpGet("{id}/classes")]
    public async Task<ActionResult<ResponseResult<List<TeacherClassAssignmentResponse>>>> GetTeacherClasses(long id)
    {
        _logger.LogInformation("Getting classes for teacher: {TeacherId}", id);

        var (success, message, data) = await _teacherService.GetTeacherClassesAsync(id);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<List<TeacherClassAssignmentResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }
}
