namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Teaching assignment management API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeachingAssignmentsController : ControllerBase
{
    private readonly ITeachingAssignmentService _service;
    private readonly ILogger<TeachingAssignmentsController> _logger;

    public TeachingAssignmentsController(ITeachingAssignmentService service, ILogger<TeachingAssignmentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all teaching assignments
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseResult<TeachingAssignmentListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting all teaching assignments: page={Page}, pageSize={PageSize}", page, pageSize);
        
        var (success, message, data) = await _service.GetAllAssignmentsAsync(page, pageSize);
        
        return Ok(new ResponseResult<TeachingAssignmentListResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get teaching assignment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<TeachingAssignmentResponse>>> GetById(long id)
    {
        _logger.LogInformation("Getting teaching assignment: {AssignmentId}", id);
        
        var (success, message, data) = await _service.GetAssignmentByIdAsync(id);
        
        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<TeachingAssignmentResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get all assignments for a specific class
    /// </summary>
    [HttpGet("class/{classId}")]
    public async Task<ActionResult<ResponseResult<List<TeacherAssignmentResponse>>>> GetByClass(long classId)
    {
        _logger.LogInformation("Getting assignments for class: {ClassId}", classId);
        
        var (success, message, data) = await _service.GetAssignmentsByClassAsync(classId);
        
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
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get all assignments for a specific teacher
    /// </summary>
    [HttpGet("teacher/{teacherId}")]
    public async Task<ActionResult<ResponseResult<List<SubjectAssignmentResponse>>>> GetByTeacher(long teacherId)
    {
        _logger.LogInformation("Getting assignments for teacher: {TeacherId}", teacherId);
        
        var (success, message, data) = await _service.GetAssignmentsByTeacherAsync(teacherId);
        
        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<List<SubjectAssignmentResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Create new teaching assignment
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResponseResult<TeachingAssignmentResponse>>> Create([FromBody] CreateTeachingAssignmentRequest request)
    {
        _logger.LogInformation("Creating teaching assignment: Class={ClassId}, Teacher={TeacherId}, Subject={SubjectId}", 
            request.ClassId, request.TeacherId, request.SubjectId);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _service.CreateAssignmentAsync(request);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = data?.Id }, new ResponseResult<TeachingAssignmentResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Update teaching assignment
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseResult<TeachingAssignmentResponse>>> Update(long id, [FromBody] UpdateTeachingAssignmentRequest request)
    {
        _logger.LogInformation("Updating teaching assignment: {AssignmentId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _service.UpdateAssignmentAsync(id, request);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<TeachingAssignmentResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Delete teaching assignment
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        _logger.LogInformation("Deleting teaching assignment: {AssignmentId}", id);

        var (success, message) = await _service.DeleteAssignmentAsync(id);

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
