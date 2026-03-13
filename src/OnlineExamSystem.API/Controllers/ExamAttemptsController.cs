using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using System.Security.Claims;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/exam-attempts")]
[Authorize]
[Produces("application/json")]
[Tags("Exam Attempts")]
public class ExamAttemptsController : ControllerBase
{
    private readonly IExamAttemptService _examAttemptService;
    private readonly IExamAttemptRepository _examAttemptRepository;
    private readonly IExamRepository _examRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;
    private readonly IExamClassRepository _examClassRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IAnswerService _answerService;
    private readonly ILogger<ExamAttemptsController> _logger;

    public ExamAttemptsController(
        IExamAttemptService examAttemptService,
        IExamAttemptRepository examAttemptRepository,
        IExamRepository examRepository,
        ITeacherRepository teacherRepository,
        ITeachingAssignmentRepository teachingAssignmentRepository,
        IExamClassRepository examClassRepository,
        IStudentRepository studentRepository,
        IAnswerService answerService,
        ILogger<ExamAttemptsController> logger)
    {
        _examAttemptService = examAttemptService;
        _examAttemptRepository = examAttemptRepository;
        _examRepository = examRepository;
        _teacherRepository = teacherRepository;
        _teachingAssignmentRepository = teachingAssignmentRepository;
        _examClassRepository = examClassRepository;
        _studentRepository = studentRepository;
        _answerService = answerService;
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

    private async Task<Student?> GetCurrentStudentAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return null;

        return await _studentRepository.GetByUserIdAsync(userId.Value);
    }

    private async Task<Teacher?> GetCurrentTeacherAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return null;

        return await _teacherRepository.GetByUserIdAsync(userId.Value);
    }

    private async Task<bool> CurrentTeacherCanAccessExamAsync(long examId)
    {
        if (User.IsInRole("ADMIN"))
            return true;

        if (!User.IsInRole("TEACHER"))
            return false;

        var teacher = await GetCurrentTeacherAsync();
        if (teacher == null)
            return false;

        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null || exam.CreatedBy != teacher.Id)
            return false;

        var examClasses = await _examClassRepository.GetExamClassesAsync(examId);
        if (examClasses.Count == 0)
            return true;

        foreach (var examClass in examClasses)
        {
            var assignment = await _teachingAssignmentRepository
                .GetByClassTeacherSubjectAsync(examClass.ClassId, teacher.Id, exam.SubjectId);
            if (assignment != null)
                return true;
        }

        return false;
    }

    private async Task<bool> CurrentUserCanAccessStudentAsync(long studentId)
    {
        if (User.IsInRole("ADMIN"))
            return true;

        if (!User.IsInRole("STUDENT"))
            return false;

        var student = await GetCurrentStudentAsync();
        return student != null && student.Id == studentId;
    }

    private async Task<bool> CurrentUserCanAccessAttemptAsync(long attemptId)
    {
        if (User.IsInRole("ADMIN"))
            return true;

        var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
        if (attempt == null)
            return false;

        if (User.IsInRole("STUDENT"))
        {
            var student = await GetCurrentStudentAsync();
            return student != null && attempt.StudentId == student.Id;
        }

        if (User.IsInRole("TEACHER"))
            return await CurrentTeacherCanAccessExamAsync(attempt.ExamId);

        return false;
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpPost("start")]
    public async Task<ActionResult<ResponseResult<ExamAttemptResponse>>> StartAttempt([FromBody] StartExamAttemptRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<ExamAttemptResponse> { Success = false, Message = "Invalid request" });

        if (User.IsInRole("STUDENT") && !await CurrentUserCanAccessStudentAsync(request.StudentId))
            return Forbid();

        if (User.IsInRole("TEACHER") && !await CurrentTeacherCanAccessExamAsync(request.ExamId))
            return Forbid();

        var (success, message, data) = await _examAttemptService.StartAttemptAsync(request.ExamId, request.StudentId);
        if (!success)
            return BadRequest(new ResponseResult<ExamAttemptResponse> { Success = false, Message = message, Data = data });

        return Ok(new ResponseResult<ExamAttemptResponse> { Success = success, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<ExamAttemptResponse>>> GetById(long id)
    {
        if (!await CurrentUserCanAccessAttemptAsync(id))
            return Forbid();

        var (success, message, data) = await _examAttemptService.GetAttemptByIdAsync(id);
        if (!success)
            return NotFound(new ResponseResult<ExamAttemptResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamAttemptResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpGet("{id}/detail")]
    public async Task<ActionResult<ResponseResult<ExamAttemptDetailResponse>>> GetDetail(long id)
    {
        if (!await CurrentUserCanAccessAttemptAsync(id))
            return Forbid();

        var (success, message, data) = await _examAttemptService.GetAttemptDetailAsync(id);
        if (!success)
            return NotFound(new ResponseResult<ExamAttemptDetailResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamAttemptDetailResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpGet]
    public async Task<ActionResult<ResponseResult<ExamAttemptListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (success, message, data) = await _examAttemptService.GetAllAttemptsAsync(page, pageSize);
        return Ok(new ResponseResult<ExamAttemptListResponse> { Success = success, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<ResponseResult<List<ExamAttemptResponse>>>> GetStudentAttempts(long studentId)
    {
        if (!await CurrentUserCanAccessStudentAsync(studentId))
            return Forbid();

        var (success, message, data) = await _examAttemptService.GetStudentAttemptsAsync(studentId);
        return Ok(new ResponseResult<List<ExamAttemptResponse>> { Success = success, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpGet("exam/{examId}")]
    public async Task<ActionResult<ResponseResult<ExamAttemptListResponse>>> GetExamAttempts(long examId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (success, message, data) = await _examAttemptService.GetExamAttemptsAsync(examId, page, pageSize);
        return Ok(new ResponseResult<ExamAttemptListResponse> { Success = success, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpGet("student/{studentId}/exam/{examId}/current")]
    public async Task<ActionResult<ResponseResult<ExamAttemptResponse>>> GetCurrentAttempt(long studentId, long examId)
    {
        if (User.IsInRole("STUDENT") && !await CurrentUserCanAccessStudentAsync(studentId))
            return Forbid();

        if (User.IsInRole("TEACHER") && !await CurrentTeacherCanAccessExamAsync(examId))
            return Forbid();

        var (success, message, data) = await _examAttemptService.GetCurrentAttemptAsync(studentId, examId);
        if (!success)
            return NotFound(new ResponseResult<ExamAttemptResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamAttemptResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<ResponseResult<SubmitExamAttemptResponse>>> Submit(long id)
    {
        if (!await CurrentUserCanAccessAttemptAsync(id))
            return Forbid();

        var (success, message, data) = await _examAttemptService.SubmitAttemptAsync(id);
        if (!success)
            return BadRequest(new ResponseResult<SubmitExamAttemptResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<SubmitExamAttemptResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpGet("{id}/questions")]
    public async Task<ActionResult<ResponseResult<List<AttemptQuestionResponse>>>> GetQuestions(long id)
    {
        if (!await CurrentUserCanAccessAttemptAsync(id))
            return Forbid();

        var (success, message, data) = await _answerService.GetAttemptQuestionsAsync(id);
        if (!success)
            return NotFound(new ResponseResult<List<AttemptQuestionResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<AttemptQuestionResponse>> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpPost("{id}/violations")]
    public async Task<ActionResult<ResponseResult<ViolationResponse>>> LogViolation(long id, [FromBody] LogViolationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<ViolationResponse> { Success = false, Message = "Invalid request" });

        if (!await CurrentUserCanAccessAttemptAsync(id))
            return Forbid();

        var (success, message, data) = await _examAttemptService.LogViolationAsync(id, request);
        if (!success)
            return BadRequest(new ResponseResult<ViolationResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ViolationResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Save answer for a question in exam attempt
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpPost("{id}/answers")]
    public async Task<ActionResult<ResponseResult<AnswerResponse>>> SaveAnswer(long id, [FromBody] SubmitAnswerRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<AnswerResponse> { Success = false, Message = "Invalid request" });

        if (!await CurrentUserCanAccessAttemptAsync(id))
            return Forbid();

        var (success, message, data) = await _answerService.SubmitAnswerAsync(id, request);
        if (!success)
            return BadRequest(new ResponseResult<AnswerResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AnswerResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Save canvas drawing answer
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpPost("{id}/canvas")]
    public async Task<ActionResult<ResponseResult<AnswerResponse>>> SaveCanvas(long id, [FromBody] SaveCanvasRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<AnswerResponse> { Success = false, Message = "Invalid request" });

        if (!await CurrentUserCanAccessAttemptAsync(id))
            return Forbid();

        var answerRequest = new SubmitAnswerRequest
        {
            QuestionId = request.QuestionId,
            CanvasImage = request.CanvasData
        };

        var (success, message, data) = await _answerService.SubmitAnswerAsync(id, answerRequest);
        if (!success)
            return BadRequest(new ResponseResult<AnswerResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AnswerResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Flag a question for review
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpPost("{id}/flag-question")]
    public async Task<ActionResult<ResponseResult<object>>> FlagQuestion(long id, [FromBody] FlagQuestionRequest request)
    {
        if (!await CurrentUserCanAccessAttemptAsync(id))
            return Forbid();

        // Flag state is managed client-side or could be persisted
        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "Question flagged",
            Data = new { AttemptId = id, request.QuestionId, Flagged = true }
        });
    }

    /// <summary>
    /// Unflag a question
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpPost("{id}/unflag-question")]
    public async Task<ActionResult<ResponseResult<object>>> UnflagQuestion(long id, [FromBody] FlagQuestionRequest request)
    {
        if (!await CurrentUserCanAccessAttemptAsync(id))
            return Forbid();

        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "Question unflagged",
            Data = new { AttemptId = id, request.QuestionId, Flagged = false }
        });
    }

    /// <summary>
    /// Resume an in-progress exam attempt
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpGet("{id}/resume")]
    public async Task<ActionResult<ResponseResult<ExamAttemptDetailResponse>>> ResumeExam(long id)
    {
        if (!await CurrentUserCanAccessAttemptAsync(id))
            return Forbid();

        var (success, message, data) = await _examAttemptService.GetAttemptDetailAsync(id);
        if (!success)
            return NotFound(new ResponseResult<ExamAttemptDetailResponse> { Success = false, Message = message });

        if (data?.Status != "IN_PROGRESS")
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Attempt is not in progress" });

        return Ok(new ResponseResult<ExamAttemptDetailResponse> { Success = true, Message = "Exam resumed", Data = data });
    }
}
