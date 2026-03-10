namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;
using OnlineExamSystem.Application.Services;

/// <summary>
/// Exam-Class assignment management API endpoints
/// </summary>
[ApiController]
[Route("api/exams/{examId}/classes")]
[Authorize]
[Produces("application/json")]
[Tags("Exam Classes")]
public class ExamClassesController : ControllerBase
{
    private readonly IExamClassService _examClassService;
    private readonly ILogger<ExamClassesController> _logger;

    public ExamClassesController(IExamClassService examClassService, ILogger<ExamClassesController> logger)
    {
        _examClassService = examClassService;
        _logger = logger;
    }

    /// <summary>
    /// Get all classes assigned to an exam
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseResult<ExamClassesResponse>>> GetExamClasses(long examId)
    {
        _logger.LogInformation("Getting classes for exam: {ExamId}", examId);
        
        var (success, message, data) = await _examClassService.GetExamClassesAsync(examId);
        
        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<ExamClassesResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Assign a class to an exam
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResponseResult<ExamClassResponse>>> AssignClass(long examId, [FromBody] long classId)
    {
        _logger.LogInformation("Assigning class {ClassId} to exam {ExamId}", classId, examId);

        var (success, message, data) = await _examClassService.AssignClassToExamAsync(examId, classId);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return CreatedAtAction(nameof(GetExamClasses), new { examId }, new ResponseResult<ExamClassResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Remove a class from an exam
    /// </summary>
    [HttpDelete("{classId}")]
    public async Task<ActionResult<ResponseResult<object>>> RemoveClass(long examId, long classId)
    {
        _logger.LogInformation("Removing class {ClassId} from exam {ExamId}", classId, examId);

        var (success, message) = await _examClassService.RemoveClassFromExamAsync(examId, classId);

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
