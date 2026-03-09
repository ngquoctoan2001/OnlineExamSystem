using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/exams/{examId}/questions")]
[Authorize]
public class ExamQuestionsController : ControllerBase
{
    private readonly IExamQuestionService _examQuestionService;
    private readonly ILogger<ExamQuestionsController> _logger;

    public ExamQuestionsController(IExamQuestionService examQuestionService, ILogger<ExamQuestionsController> logger)
    {
        _examQuestionService = examQuestionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ResponseResult<ExamQuestionDetailResponse>>> AddQuestion(long examId, [FromBody] AddQuestionToExamRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<ExamQuestionDetailResponse> { Success = false, Message = "Invalid request" });

        request.ExamId = examId;
        var (success, message, data) = await _examQuestionService.AddQuestionToExamAsync(request);
        
        if (!success)
            return BadRequest(new ResponseResult<ExamQuestionDetailResponse> { Success = false, Message = message });

        return CreatedAtAction(nameof(GetQuestion), new { examId, id = data?.Id }, new ResponseResult<ExamQuestionDetailResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet]
    public async Task<ActionResult<ResponseResult<ExamQuestionsListResponse>>> GetQuestions(long examId)
    {
        var (success, message, data) = await _examQuestionService.GetExamQuestionsAsync(examId);
        
        if (!success)
            return NotFound(new ResponseResult<ExamQuestionsListResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamQuestionsListResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<ExamQuestionDetailResponse>>> GetQuestion(long examId, long id)
    {
        var (success, message, data) = await _examQuestionService.GetExamQuestionByIdAsync(id);
        
        if (!success)
            return NotFound(new ResponseResult<ExamQuestionDetailResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamQuestionDetailResponse> { Success = true, Message = message, Data = data });
    }

    [HttpDelete("{questionId}")]
    public async Task<ActionResult<ResponseResult<string>>> RemoveQuestion(long examId, long questionId)
    {
        var (success, message) = await _examQuestionService.RemoveQuestionFromExamAsync(examId, questionId);
        
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Question removed" });
    }

    [HttpPost("reorder")]
    public async Task<ActionResult<ResponseResult<string>>> ReorderQuestions(long examId, [FromBody] ReorderExamQuestionsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<string> { Success = false, Message = "Invalid request" });

        var (success, message) = await _examQuestionService.ReorderQuestionsAsync(examId, request);
        
        if (!success)
            return BadRequest(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Questions reordered" });
    }

    [HttpPost("{examQuestionId}/max-score")]
    public async Task<ActionResult<ResponseResult<string>>> UpdateMaxScore(long examId, long examQuestionId, [FromBody] JsonElement body)
    {
        if (!body.TryGetProperty("maxScore", out var scoreElement) || !scoreElement.TryGetInt32(out var maxScore))
            return BadRequest(new ResponseResult<string> { Success = false, Message = "Valid maxScore required" });

        var (success, message) = await _examQuestionService.UpdateQuestionMaxScoreAsync(examQuestionId, maxScore);
        
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Max score updated" });
    }
}
