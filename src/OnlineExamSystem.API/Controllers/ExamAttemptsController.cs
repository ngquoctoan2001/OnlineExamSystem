using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/exam-attempts")]
[Authorize]
[Produces("application/json")]
[Tags("Exam Attempts")]
public class ExamAttemptsController : ControllerBase
{
    private readonly IExamAttemptService _examAttemptService;
    private readonly IAnswerService _answerService;
    private readonly ILogger<ExamAttemptsController> _logger;

    public ExamAttemptsController(IExamAttemptService examAttemptService, IAnswerService answerService, ILogger<ExamAttemptsController> logger)
    {
        _examAttemptService = examAttemptService;
        _answerService = answerService;
        _logger = logger;
    }

    [HttpPost("start")]
    public async Task<ActionResult<ResponseResult<ExamAttemptResponse>>> StartAttempt([FromBody] StartExamAttemptRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<ExamAttemptResponse> { Success = false, Message = "Invalid request" });

        var (success, message, data) = await _examAttemptService.StartAttemptAsync(request.ExamId, request.StudentId);
        return Ok(new ResponseResult<ExamAttemptResponse> { Success = success, Message = message, Data = data });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<ExamAttemptResponse>>> GetById(long id)
    {
        var (success, message, data) = await _examAttemptService.GetAttemptByIdAsync(id);
        if (!success)
            return NotFound(new ResponseResult<ExamAttemptResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamAttemptResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet("{id}/detail")]
    public async Task<ActionResult<ResponseResult<ExamAttemptDetailResponse>>> GetDetail(long id)
    {
        var (success, message, data) = await _examAttemptService.GetAttemptDetailAsync(id);
        if (!success)
            return NotFound(new ResponseResult<ExamAttemptDetailResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamAttemptDetailResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet]
    public async Task<ActionResult<ResponseResult<ExamAttemptListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (success, message, data) = await _examAttemptService.GetAllAttemptsAsync(page, pageSize);
        return Ok(new ResponseResult<ExamAttemptListResponse> { Success = success, Message = message, Data = data });
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<ResponseResult<List<ExamAttemptResponse>>>> GetStudentAttempts(long studentId)
    {
        var (success, message, data) = await _examAttemptService.GetStudentAttemptsAsync(studentId);
        return Ok(new ResponseResult<List<ExamAttemptResponse>> { Success = success, Message = message, Data = data });
    }

    [HttpGet("exam/{examId}")]
    public async Task<ActionResult<ResponseResult<ExamAttemptListResponse>>> GetExamAttempts(long examId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (success, message, data) = await _examAttemptService.GetExamAttemptsAsync(examId, page, pageSize);
        return Ok(new ResponseResult<ExamAttemptListResponse> { Success = success, Message = message, Data = data });
    }

    [HttpGet("student/{studentId}/exam/{examId}/current")]
    public async Task<ActionResult<ResponseResult<ExamAttemptResponse>>> GetCurrentAttempt(long studentId, long examId)
    {
        var (success, message, data) = await _examAttemptService.GetCurrentAttemptAsync(studentId, examId);
        if (!success)
            return NotFound(new ResponseResult<ExamAttemptResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamAttemptResponse> { Success = true, Message = message, Data = data });
    }

    [HttpPost("{id}/submit")]
    public async Task<ActionResult<ResponseResult<SubmitExamAttemptResponse>>> Submit(long id)
    {
        var (success, message, data) = await _examAttemptService.SubmitAttemptAsync(id);
        if (!success)
            return BadRequest(new ResponseResult<SubmitExamAttemptResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<SubmitExamAttemptResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet("{id}/questions")]
    public async Task<ActionResult<ResponseResult<List<AttemptQuestionResponse>>>> GetQuestions(long id)
    {
        var (success, message, data) = await _answerService.GetAttemptQuestionsAsync(id);
        if (!success)
            return NotFound(new ResponseResult<List<AttemptQuestionResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<AttemptQuestionResponse>> { Success = true, Message = message, Data = data });
    }

    [HttpPost("{id}/violations")]
    public async Task<ActionResult<ResponseResult<ViolationResponse>>> LogViolation(long id, [FromBody] LogViolationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<ViolationResponse> { Success = false, Message = "Invalid request" });

        var (success, message, data) = await _examAttemptService.LogViolationAsync(id, request);
        if (!success)
            return BadRequest(new ResponseResult<ViolationResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ViolationResponse> { Success = true, Message = message, Data = data });
    }
}
