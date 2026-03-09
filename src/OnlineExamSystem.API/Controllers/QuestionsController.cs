using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/questions")]
[Authorize]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(IQuestionService questionService, ILogger<QuestionsController> logger)
    {
        _questionService = questionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ResponseResult<QuestionDetailResponse>>> CreateQuestion([FromBody] CreateQuestionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<QuestionDetailResponse> { Success = false, Message = "Invalid request" });

        var userId = long.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        var (success, message, data) = await _questionService.CreateQuestionAsync(request, userId);
        return Ok(new ResponseResult<QuestionDetailResponse> { Success = success, Message = message, Data = data });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<QuestionDetailResponse>>> GetById(long id)
    {
        var (success, message, data) = await _questionService.GetQuestionByIdAsync(id);
        if (!success)
            return NotFound(new ResponseResult<QuestionDetailResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<QuestionDetailResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet]
    public async Task<ActionResult<ResponseResult<QuestionListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (success, message, data) = await _questionService.GetAllQuestionsAsync(page, pageSize);
        return Ok(new ResponseResult<QuestionListResponse> { Success = success, Message = message, Data = data });
    }

    [HttpGet("published")]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseResult<QuestionListResponse>>> GetPublished([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (success, message, data) = await _questionService.GetPublishedQuestionsAsync(page, pageSize);
        return Ok(new ResponseResult<QuestionListResponse> { Success = success, Message = message, Data = data });
    }

    [HttpGet("search")]
    public async Task<ActionResult<ResponseResult<List<QuestionResponse>>>> Search([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new ResponseResult<List<QuestionResponse>> { Success = false, Message = "Search term required" });

        var (success, message, data) = await _questionService.SearchQuestionsAsync(term);
        return Ok(new ResponseResult<List<QuestionResponse>> { Success = success, Message = message, Data = data });
    }

    [HttpGet("subject/{subjectId}")]
    public async Task<ActionResult<ResponseResult<List<QuestionResponse>>>> GetBySubject(long subjectId)
    {
        var (success, message, data) = await _questionService.GetQuestionsBySubjectAsync(subjectId);
        return Ok(new ResponseResult<List<QuestionResponse>> { Success = success, Message = message, Data = data });
    }

    [HttpGet("difficulty/{difficulty}")]
    public async Task<ActionResult<ResponseResult<List<QuestionResponse>>>> GetByDifficulty(string difficulty)
    {
        var (success, message, data) = await _questionService.GetQuestionsByDifficultyAsync(difficulty);
        if (!success)
            return BadRequest(new ResponseResult<List<QuestionResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<QuestionResponse>> { Success = true, Message = message, Data = data });
    }

    [HttpGet("type/{questionTypeId}")]
    public async Task<ActionResult<ResponseResult<List<QuestionResponse>>>> GetByType(long questionTypeId)
    {
        var (success, message, data) = await _questionService.GetQuestionsByTypeAsync(questionTypeId);
        return Ok(new ResponseResult<List<QuestionResponse>> { Success = success, Message = message, Data = data });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseResult<QuestionDetailResponse>>> UpdateQuestion(long id, [FromBody] UpdateQuestionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<QuestionDetailResponse> { Success = false, Message = "Invalid request" });

        var (success, message, data) = await _questionService.UpdateQuestionAsync(id, request);
        if (!success)
            return NotFound(new ResponseResult<QuestionDetailResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<QuestionDetailResponse> { Success = true, Message = message, Data = data });
    }

    [HttpPost("{id}/publish")]
    public async Task<ActionResult<ResponseResult<string>>> PublishQuestion(long id)
    {
        var (success, message) = await _questionService.PublishQuestionAsync(id);
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Question published" });
    }

    [HttpPost("{id}/unpublish")]
    public async Task<ActionResult<ResponseResult<string>>> UnpublishQuestion(long id)
    {
        var (success, message) = await _questionService.UnpublishQuestionAsync(id);
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Question unpublished" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<string>>> DeleteQuestion(long id)
    {
        var (success, message) = await _questionService.DeleteQuestionAsync(id);
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Question deleted" });
    }
}
