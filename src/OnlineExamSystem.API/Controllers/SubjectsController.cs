namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Subject management API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Subjects")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    private readonly ILogger<SubjectsController> _logger;

    public SubjectsController(ISubjectService subjectService, ILogger<SubjectsController> logger)
    {
        _subjectService = subjectService;
        _logger = logger;
    }

    /// <summary>
    /// Get all subjects
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseResult<SubjectListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting all subjects: page={Page}, pageSize={PageSize}", page, pageSize);
        
        var (success, message, data) = await _subjectService.GetAllSubjectsAsync(page, pageSize);
        
        return Ok(new ResponseResult<SubjectListResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get subject by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<SubjectResponse>>> GetById(long id)
    {
        _logger.LogInformation("Getting subject: {SubjectId}", id);
        
        var (success, message, data) = await _subjectService.GetSubjectByIdAsync(id);
        
        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<SubjectResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Search subjects by name or code
    /// </summary>
    [HttpGet("search/{searchTerm}")]
    public async Task<ActionResult<ResponseResult<List<SubjectResponse>>>> Search(string searchTerm)
    {
        _logger.LogInformation("Searching subjects: {SearchTerm}", searchTerm);
        
        var (success, message, data) = await _subjectService.SearchSubjectsAsync(searchTerm);
        
        return Ok(new ResponseResult<List<SubjectResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Create new subject
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResponseResult<SubjectResponse>>> Create([FromBody] CreateSubjectRequest request)
    {
        _logger.LogInformation("Creating new subject: {Code}", request.Code);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _subjectService.CreateSubjectAsync(request);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = data?.Id }, new ResponseResult<SubjectResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Update subject information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseResult<SubjectResponse>>> Update(long id, [FromBody] UpdateSubjectRequest request)
    {
        _logger.LogInformation("Updating subject: {SubjectId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _subjectService.UpdateSubjectAsync(id, request);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<SubjectResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Delete subject
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        _logger.LogInformation("Deleting subject: {SubjectId}", id);

        var (success, message) = await _subjectService.DeleteSubjectAsync(id);

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
