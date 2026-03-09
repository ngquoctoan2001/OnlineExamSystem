namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Application.Services;

/// <summary>
/// Exam management API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly ILogger<ExamsController> _logger;

    public ExamsController(IExamService examService, ILogger<ExamsController> logger)
    {
        _examService = examService;
        _logger = logger;
    }

    /// <summary>
    /// Get all exams
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseResult<ExamListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting all exams: page={Page}, pageSize={PageSize}", page, pageSize);
        
        var (success, message, data) = await _examService.GetAllExamsAsync(page, pageSize);
        
        return Ok(new ResponseResult<ExamListResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get exam by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<ExamResponse>>> GetById(long id)
    {
        _logger.LogInformation("Getting exam: {ExamId}", id);
        
        var (success, message, data) = await _examService.GetExamByIdAsync(id);
        
        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<ExamResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Search exams by title or description
    /// </summary>
    [HttpGet("search/{searchTerm}")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> Search(string searchTerm)
    {
        _logger.LogInformation("Searching exams: {SearchTerm}", searchTerm);
        
        var (success, message, data) = await _examService.SearchExamsAsync(searchTerm);
        
        return Ok(new ResponseResult<List<ExamResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get exams by teacher
    /// </summary>
    [HttpGet("teacher/{teacherId}")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetByTeacher(long teacherId)
    {
        _logger.LogInformation("Getting exams by teacher: {TeacherId}", teacherId);
        
        var (success, message, data) = await _examService.GetExamsByTeacherAsync(teacherId);
        
        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<List<ExamResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get exams by subject
    /// </summary>
    [HttpGet("subject/{subjectId}")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetBySubject(long subjectId)
    {
        _logger.LogInformation("Getting exams by subject: {SubjectId}", subjectId);
        
        var (success, message, data) = await _examService.GetExamsBySubjectAsync(subjectId);
        
        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<List<ExamResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Create new exam
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResponseResult<ExamResponse>>> Create([FromBody] CreateExamRequest request)
    {
        _logger.LogInformation("Creating new exam: {Title}", request.Title);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _examService.CreateExamAsync(request);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = data?.Id }, new ResponseResult<ExamResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Update exam
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseResult<ExamResponse>>> Update(long id, [FromBody] UpdateExamRequest request)
    {
        _logger.LogInformation("Updating exam: {ExamId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _examService.UpdateExamAsync(id, request);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<ExamResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Delete exam
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        _logger.LogInformation("Deleting exam: {ExamId}", id);

        var (success, message) = await _examService.DeleteExamAsync(id);

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
}
