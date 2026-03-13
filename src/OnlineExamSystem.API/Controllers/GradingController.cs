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
[Route("api/grading")]
[Authorize]
[Produces("application/json")]
[Tags("Grading")]
public class GradingController : ControllerBase
{
    private readonly IGradingService _gradingService;
    private readonly IExamAttemptService _examAttemptService;
    private readonly IExamAttemptRepository _examAttemptRepository;
    private readonly IExamRepository _examRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;
    private readonly IExamClassRepository _examClassRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<GradingController> _logger;

    public GradingController(
        IGradingService gradingService,
        IExamAttemptService examAttemptService,
        IExamAttemptRepository examAttemptRepository,
        IExamRepository examRepository,
        ITeacherRepository teacherRepository,
        ITeachingAssignmentRepository teachingAssignmentRepository,
        IExamClassRepository examClassRepository,
        IStudentRepository studentRepository,
        ILogger<GradingController> logger)
    {
        _gradingService = gradingService;
        _examAttemptService = examAttemptService;
        _examAttemptRepository = examAttemptRepository;
        _examRepository = examRepository;
        _teacherRepository = teacherRepository;
        _teachingAssignmentRepository = teachingAssignmentRepository;
        _examClassRepository = examClassRepository;
        _studentRepository = studentRepository;
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

    private async Task<bool> CurrentTeacherCanAccessAttemptAsync(long attemptId)
    {
        var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
        if (attempt == null)
            return false;

        return await CurrentTeacherCanAccessExamAsync(attempt.ExamId);
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("auto-grade/{attemptId}")]
    public async Task<ActionResult<ResponseResult<List<GradingResultResponse>>>> AutoGrade(long attemptId)
    {
        if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
            return Forbid();

        var (success, message, data) = await _gradingService.AutoGradeAttemptAsync(attemptId);
        if (!success)
            return BadRequest(new ResponseResult<List<GradingResultResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<GradingResultResponse>> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpGet("attempts/{attemptId}/view")]
    public async Task<ActionResult<ResponseResult<AttemptGradingViewResponse>>> GetGradingView(long attemptId)
    {
        if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
            return Forbid();

        var (success, message, data) = await _gradingService.GetAttemptGradingViewAsync(attemptId);
        if (!success)
            return NotFound(new ResponseResult<AttemptGradingViewResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AttemptGradingViewResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpGet("exams/{examId}/pending")]
    public async Task<ActionResult<ResponseResult<List<PendingGradingAttemptResponse>>>> GetPending(long examId)
    {
        if (!await CurrentTeacherCanAccessExamAsync(examId))
            return Forbid();

        var (success, message, data) = await _gradingService.GetPendingGradingAsync(examId);
        if (!success)
            return NotFound(new ResponseResult<List<PendingGradingAttemptResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<PendingGradingAttemptResponse>> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPut("attempts/{attemptId}/questions/{questionId}")]
    public async Task<ActionResult<ResponseResult<GradingResultResponse>>> ManualGrade(long attemptId, long questionId, [FromBody] ManualGradeRequest request)
    {
        if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<GradingResultResponse> { Success = false, Message = "Invalid request" });

        // Use teacher ID from claims; fall back to 0 if not present
        var gradedBy = GetCurrentUserId() ?? 0L;
        var (success, message, data) = await _gradingService.ManualGradeQuestionAsync(attemptId, questionId, request, gradedBy);
        if (!success)
            return BadRequest(new ResponseResult<GradingResultResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<GradingResultResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPut("attempts/{attemptId}/batch-grade")]
    public async Task<ActionResult<ResponseResult<List<GradingResultResponse>>>> BatchGrade(long attemptId, [FromBody] BatchGradeRequest request)
    {
        if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<List<GradingResultResponse>> { Success = false, Message = "Invalid request" });

        var gradedBy = GetCurrentUserId() ?? 0L;
        var (success, message, data) = await _gradingService.BatchGradeAsync(attemptId, request, gradedBy);
        if (!success)
            return BadRequest(new ResponseResult<List<GradingResultResponse>> { Success = false, Message = message });

        return Ok(new ResponseResult<List<GradingResultResponse>> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("attempts/{attemptId}/mark-graded")]
    public async Task<ActionResult<ResponseResult<object>>> MarkAsGraded(long attemptId)
    {
        if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
            return Forbid();

        var (success, message) = await _gradingService.MarkAsGradedAsync(attemptId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("attempts/{attemptId}/publish")]
    public async Task<ActionResult<ResponseResult<PublishResultResponse>>> Publish(long attemptId)
    {
        if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
            return Forbid();

        var (success, message, data) = await _gradingService.PublishResultAsync(attemptId);
        if (!success)
            return BadRequest(new ResponseResult<PublishResultResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<PublishResultResponse> { Success = true, Message = message, Data = data });
    }

    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    [HttpGet("attempts/{attemptId}/result")]
    public async Task<ActionResult<ResponseResult<AttemptGradingViewResponse>>> GetStudentResult(long attemptId)
    {
        if (User.IsInRole("STUDENT"))
        {
            var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
                return NotFound(new ResponseResult<AttemptGradingViewResponse> { Success = false, Message = "Exam attempt not found" });

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Forbid();

            var student = await _studentRepository.GetByUserIdAsync(userId.Value);
            if (student == null || attempt.StudentId != student.Id)
                return Forbid();
        }
        else if (User.IsInRole("TEACHER"))
        {
            if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
                return Forbid();
        }

        var (success, message, data) = await _gradingService.GetStudentResultAsync(attemptId);
        if (!success)
            return NotFound(new ResponseResult<AttemptGradingViewResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AttemptGradingViewResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Get all attempts for an exam (for grading)
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpGet("exams/{examId}/attempts")]
    public async Task<ActionResult<ResponseResult<ExamAttemptListResponse>>> GetExamAttempts(long examId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await CurrentTeacherCanAccessExamAsync(examId))
            return Forbid();

        var (success, message, data) = await _examAttemptService.GetExamAttemptsAsync(examId, page, pageSize);
        if (!success)
            return NotFound(new ResponseResult<ExamAttemptListResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamAttemptListResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Get attempt detail for grading
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpGet("attempts/{attemptId}")]
    public async Task<ActionResult<ResponseResult<AttemptGradingViewResponse>>> GetAttemptDetail(long attemptId)
    {
        if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
            return Forbid();

        var (success, message, data) = await _gradingService.GetAttemptGradingViewAsync(attemptId);
        if (!success)
            return NotFound(new ResponseResult<AttemptGradingViewResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<AttemptGradingViewResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Grade a specific question in an attempt
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("attempts/{attemptId}/score")]
    public async Task<ActionResult<ResponseResult<GradingResultResponse>>> GradeQuestion(long attemptId, [FromBody] GradeQuestionRequest request)
    {
        if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<GradingResultResponse> { Success = false, Message = "Invalid request" });

        var gradedBy = GetCurrentUserId() ?? 0L;
        var manualRequest = new ManualGradeRequest { Score = request.Score, Comment = request.Comment };
        var (success, message, data) = await _gradingService.ManualGradeQuestionAsync(attemptId, request.QuestionId, manualRequest, gradedBy);
        if (!success)
            return BadRequest(new ResponseResult<GradingResultResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<GradingResultResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Add annotation to an attempt
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("attempts/{attemptId}/annotation")]
    public async Task<ActionResult<ResponseResult<object>>> AddAnnotation(long attemptId, [FromBody] AddAnnotationRequest request)
    {
        if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(new ResponseResult<object> { Success = false, Message = "Invalid request" });

        // Annotation is stored as a comment on the grading result
        var gradedBy = GetCurrentUserId() ?? 0L;
        var manualRequest = new ManualGradeRequest { Score = 0, Comment = request.Content };
        await _gradingService.ManualGradeQuestionAsync(attemptId, request.QuestionId, manualRequest, gradedBy);

        return Ok(new ResponseResult<object>
        {
            Success = true,
            Message = "Annotation added",
            Data = new { attemptId, request.QuestionId, request.Content, request.Type }
        });
    }

    /// <summary>
    /// Finalize grading for an attempt
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("attempts/{attemptId}/finalize")]
    public async Task<ActionResult<ResponseResult<object>>> FinalizeGrading(long attemptId)
    {
        if (!await CurrentTeacherCanAccessAttemptAsync(attemptId))
            return Forbid();

        var (success, message) = await _gradingService.MarkAsGradedAsync(attemptId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }
}
