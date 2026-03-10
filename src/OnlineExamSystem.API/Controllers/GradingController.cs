using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/grading")]
[Authorize]
[Produces("application/json")]
[Tags("Grading")]
public class GradingController : ControllerBase
{
    private readonly IGradingService _gradingService;
    private readonly ILogger<GradingController> _logger;

    public GradingController(IGradingService gradingService, ILogger<GradingController> logger)
    {
        _gradingService = gradingService;
        _logger = logger;
    }

    [HttpPost("auto-grade/{attemptId}")]
    public async Task<ActionResult<ResponseResult<List<GradingResultResponse>>>> AutoGrade(long attemptId)
    {
        var (success, message, data) = await _gradingService.AutoGradeAttemptAsync(attemptId);
        if (!success)
            return BadRequest(new ResponseResult<List<GradingResultResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<GradingResultResponse>> { Success = true, Message = message, Data = data });
    }

    [HttpGet("attempts/{attemptId}/view")]
    public async Task<ActionResult<ResponseResult<AttemptGradingViewResponse>>> GetGradingView(long attemptId)
    {
        var (success, message, data) = await _gradingService.GetAttemptGradingViewAsync(attemptId);
        if (!success)
            return NotFound(new ResponseResult<AttemptGradingViewResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AttemptGradingViewResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet("exams/{examId}/pending")]
    public async Task<ActionResult<ResponseResult<List<PendingGradingAttemptResponse>>>> GetPending(long examId)
    {
        var (success, message, data) = await _gradingService.GetPendingGradingAsync(examId);
        if (!success)
            return NotFound(new ResponseResult<List<PendingGradingAttemptResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<PendingGradingAttemptResponse>> { Success = true, Message = message, Data = data });
    }

    [HttpPut("attempts/{attemptId}/questions/{questionId}")]
    public async Task<ActionResult<ResponseResult<GradingResultResponse>>> ManualGrade(long attemptId, long questionId, [FromBody] ManualGradeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<GradingResultResponse> { Success = false, Message = "Invalid request" });

        // Use teacher ID from claims; fall back to 0 if not present
        var gradedBy = long.TryParse(User.FindFirst("userId")?.Value, out var uid) ? uid : 0L;
        var (success, message, data) = await _gradingService.ManualGradeQuestionAsync(attemptId, questionId, request, gradedBy);
        if (!success)
            return BadRequest(new ResponseResult<GradingResultResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<GradingResultResponse> { Success = true, Message = message, Data = data });
    }

    [HttpPost("attempts/{attemptId}/mark-graded")]
    public async Task<ActionResult<ResponseResult<object>>> MarkAsGraded(long attemptId)
    {
        var (success, message) = await _gradingService.MarkAsGradedAsync(attemptId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    [HttpPost("attempts/{attemptId}/publish")]
    public async Task<ActionResult<ResponseResult<PublishResultResponse>>> Publish(long attemptId)
    {
        var (success, message, data) = await _gradingService.PublishResultAsync(attemptId);
        if (!success)
            return BadRequest(new ResponseResult<PublishResultResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<PublishResultResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet("attempts/{attemptId}/result")]
    public async Task<ActionResult<ResponseResult<AttemptGradingViewResponse>>> GetStudentResult(long attemptId)
    {
        var (success, message, data) = await _gradingService.GetStudentResultAsync(attemptId);
        if (!success)
            return NotFound(new ResponseResult<AttemptGradingViewResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AttemptGradingViewResponse> { Success = true, Message = message, Data = data });
    }
}
