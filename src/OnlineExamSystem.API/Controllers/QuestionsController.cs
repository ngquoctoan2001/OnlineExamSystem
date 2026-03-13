using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using System.Security.Claims;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/questions")]
[Authorize]
[Produces("application/json")]
[Tags("Questions")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IQuestionOptionRepository _questionOptionRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(
        IQuestionService questionService,
        IQuestionOptionRepository questionOptionRepository,
        ITeacherRepository teacherRepository,
        ITeachingAssignmentRepository teachingAssignmentRepository,
        ILogger<QuestionsController> logger)
    {
        _questionService = questionService;
        _questionOptionRepository = questionOptionRepository;
        _teacherRepository = teacherRepository;
        _teachingAssignmentRepository = teachingAssignmentRepository;
        _logger = logger;
    }

    private long? GetCurrentUserId()
    {
        var claim = User.FindFirst("userId")?.Value
                    ?? User.FindFirst("UserId")?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(claim, out var id) ? id : null;
    }

    private async Task<Teacher?> GetCurrentTeacherAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return null;
        return await _teacherRepository.GetByUserIdAsync(userId.Value);
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
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
    public async Task<ActionResult<ResponseResult<QuestionListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] long? tagId = null)
    {
        if (tagId.HasValue)
        {
            var (tagSuccess, tagMessage, tagData) = await _questionService.GetQuestionsByTagAsync(tagId.Value);
            var tagListResponse = new QuestionListResponse
            {
                Items = tagData ?? new(),
                Page = 1,
                PageSize = tagData?.Count ?? 0,
                TotalCount = tagData?.Count ?? 0
            };
            return Ok(new ResponseResult<QuestionListResponse> { Success = tagSuccess, Message = tagMessage, Data = tagListResponse });
        }

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
    public async Task<ActionResult<ResponseResult<List<QuestionResponse>>>> GetBySubject(long subjectId, [FromQuery] long? createdByTeacherId = null)
    {
        // If teacher is requesting and they haven't specified a teacher ID, filter by their own ID
        if (!createdByTeacherId.HasValue && User.IsInRole("TEACHER"))
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher != null)
            {
                createdByTeacherId = teacher.Id;
            }
        }

        // If a teacher ID is specified, filter by that teacher
        if (createdByTeacherId.HasValue)
        {
            var (success, message, data) = await _questionService.GetQuestionsByTeacherAndSubjectAsync(createdByTeacherId.Value, subjectId);
            return Ok(new ResponseResult<List<QuestionResponse>> { Success = success, Message = message, Data = data });
        }

        // Otherwise, return all questions from this subject
        var (allSuccess, allMessage, allData) = await _questionService.GetQuestionsBySubjectAsync(subjectId);
        return Ok(new ResponseResult<List<QuestionResponse>> { Success = allSuccess, Message = allMessage, Data = allData });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpGet("teacher/my-questions")]
    public async Task<ActionResult<ResponseResult<List<QuestionResponse>>>> GetMyQuestions()
    {
        var teacher = await GetCurrentTeacherAsync();
        if (teacher == null)
        {
            return Unauthorized(new ResponseResult<List<QuestionResponse>>
            {
                Success = false,
                Message = "Teacher profile not found"
            });
        }

        var (success, message, data) = await _questionService.GetQuestionsByTeacherAsync(teacher.Id);
        return Ok(new ResponseResult<List<QuestionResponse>> { Success = success, Message = message, Data = data });
    }
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

    [Authorize(Roles = "ADMIN,TEACHER")]
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

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{id}/publish")]
    public async Task<ActionResult<ResponseResult<string>>> PublishQuestion(long id)
    {
        var (success, message) = await _questionService.PublishQuestionAsync(id);
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Question published" });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{id}/unpublish")]
    public async Task<ActionResult<ResponseResult<string>>> UnpublishQuestion(long id)
    {
        var (success, message) = await _questionService.UnpublishQuestionAsync(id);
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Question unpublished" });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<string>>> DeleteQuestion(long id)
    {
        var (success, message) = await _questionService.DeleteQuestionAsync(id);
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Question deleted" });
    }

    // ─── Question Options ─────────────────────────────────────────────────────

    [HttpGet("{id}/options")]
    public async Task<ActionResult<ResponseResult<List<QuestionOptionResponse>>>> GetOptions(long id)
    {
        var (success, message, data) = await _questionService.GetQuestionByIdAsync(id);
        if (!success)
            return NotFound(new ResponseResult<List<QuestionOptionResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<QuestionOptionResponse>>
        {
            Success = true,
            Message = "Success",
            Data = data!.Options
        });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{id}/options")]
    public async Task<ActionResult<ResponseResult<QuestionOptionResponse>>> AddOption(long id, [FromBody] CreateQuestionOptionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<QuestionOptionResponse> { Success = false, Message = "Invalid request" });

        var (success, message, data) = await _questionService.AddOptionAsync(id, request);
        if (!success)
            return BadRequest(new ResponseResult<QuestionOptionResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<QuestionOptionResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPut("{id}/options/{optionId}")]
    public async Task<ActionResult<ResponseResult<QuestionOptionResponse>>> UpdateOption(long id, long optionId, [FromBody] CreateQuestionOptionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<QuestionOptionResponse> { Success = false, Message = "Invalid request" });

        var (success, message, data) = await _questionService.UpdateOptionAsync(id, optionId, request);
        if (!success)
            return NotFound(new ResponseResult<QuestionOptionResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<QuestionOptionResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpDelete("{id}/options/{optionId}")]
    public async Task<ActionResult<ResponseResult<string>>> DeleteOption(long id, long optionId)
    {
        var (success, message) = await _questionService.DeleteOptionAsync(id, optionId);
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Option deleted" });
    }

    // ─── Question Tags ────────────────────────────────────────────────────────

    [HttpGet("{id}/tags")]
    public async Task<ActionResult<ResponseResult<List<TagResponse>>>> GetTags(long id)
    {
        var (success, message, data) = await _questionService.GetQuestionTagsAsync(id);
        if (!success)
            return NotFound(new ResponseResult<List<TagResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<TagResponse>> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{id}/tags/{tagId}")]
    public async Task<ActionResult<ResponseResult<string>>> AssignTag(long id, long tagId)
    {
        var (success, message) = await _questionService.AssignTagAsync(id, tagId);
        if (!success)
            return BadRequest(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Tag assigned" });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpDelete("{id}/tags/{tagId}")]
    public async Task<ActionResult<ResponseResult<string>>> RemoveTag(long id, long tagId)
    {
        var (success, message) = await _questionService.RemoveTagAsync(id, tagId);
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Tag removed" });
    }

    // ─── Question Option shortcuts (by optionId only) ─────────────────────────

    /// <summary>
    /// Update question option by option ID
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPut("options/{optionId}")]
    public async Task<ActionResult<ResponseResult<QuestionOptionResponse>>> UpdateOptionByOptionId(long optionId, [FromBody] CreateQuestionOptionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<QuestionOptionResponse> { Success = false, Message = "Invalid request" });

        // Find the question that owns this option
        var option = await _questionOptionRepository.GetByIdAsync(optionId);
        if (option == null)
            return NotFound(new ResponseResult<QuestionOptionResponse> { Success = false, Message = "Option not found" });

        var (success, message, data) = await _questionService.UpdateOptionAsync(option.QuestionId, optionId, request);
        if (!success)
            return NotFound(new ResponseResult<QuestionOptionResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<QuestionOptionResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Delete question option by option ID
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpDelete("options/{optionId}")]
    public async Task<ActionResult<ResponseResult<string>>> DeleteOptionByOptionId(long optionId)
    {
        var option = await _questionOptionRepository.GetByIdAsync(optionId);
        if (option == null)
            return NotFound(new ResponseResult<string> { Success = false, Message = "Option not found" });

        var (success, message) = await _questionService.DeleteOptionAsync(option.QuestionId, optionId);
        if (!success)
            return NotFound(new ResponseResult<string> { Success = false, Message = message });

        return Ok(new ResponseResult<string> { Success = true, Message = message, Data = "Option deleted" });
    }

    // ─── Import endpoints ─────────────────────────────────────────────────────

    /// <summary>
    /// Import questions from PDF
    /// </summary>
    [HttpPost("import/pdf")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<object>>> ImportFromPdf()
    {
        // PDF import would require a PDF parser - placeholder for now
        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "PDF import endpoint available. Upload PDF file with questions."
        });
    }

    /// <summary>
    /// Import questions from Word document
    /// </summary>
    [HttpPost("import/docx")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<object>>> ImportFromDocx()
    {
        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "DOCX import endpoint available. Upload Word file with questions."
        });
    }

    /// <summary>
    /// Import questions from LaTeX
    /// </summary>
    [HttpPost("import/latex")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<object>>> ImportFromLatex()
    {
        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "LaTeX import endpoint available. Upload LaTeX file with questions."
        });
    }

    /// <summary>
    /// Import questions from Excel
    /// </summary>
    [HttpPost("import/excel")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<object>>> ImportFromExcel()
    {
        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "Excel import endpoint available. Upload Excel file with questions."
        });
    }
}
