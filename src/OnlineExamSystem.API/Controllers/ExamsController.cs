namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using IExamService = OnlineExamSystem.Application.Services.IExamService;

/// <summary>
/// Exam management API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Exams")]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly IExamRepository _examRepository;
    private readonly IExamClassRepository _examClassRepository;
    private readonly IExamClassService _examClassService;
    private readonly IExamQuestionService _examQuestionService;
    private readonly IStudentRepository _studentRepository;
    private readonly IExamAttemptRepository _examAttemptRepository;
    private readonly ILogger<ExamsController> _logger;

    public ExamsController(
        IExamService examService,
        IExamRepository examRepository,
        IExamClassRepository examClassRepository,
        IExamClassService examClassService,
        IExamQuestionService examQuestionService,
        IStudentRepository studentRepository,
        IExamAttemptRepository examAttemptRepository,
        ILogger<ExamsController> logger)
    {
        _examService = examService;
        _examRepository = examRepository;
        _examClassRepository = examClassRepository;
        _examClassService = examClassService;
        _examQuestionService = examQuestionService;
        _studentRepository = studentRepository;
        _examAttemptRepository = examAttemptRepository;
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
    /// Get exams assigned to a class
    /// </summary>
    [HttpGet("class/{classId}")]
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetByClass(long classId)
    {
        var examClasses = await _examClassRepository.GetClassExamsAsync(classId);
        var exams = new List<ExamResponse>();

        foreach (var examClass in examClasses)
        {
            var exam = await _examRepository.GetByIdAsync(examClass.ExamId);
            if (exam == null)
                continue;

            exams.Add(new ExamResponse
            {
                Id = exam.Id,
                Title = exam.Title,
                SubjectId = exam.SubjectId,
                SubjectName = exam.Subject?.Name ?? string.Empty,
                CreatedBy = exam.CreatedBy,
                DurationMinutes = exam.DurationMinutes,
                StartTime = exam.StartTime,
                EndTime = exam.EndTime,
                Description = exam.Description,
                Status = exam.Status,
                CreatedAt = exam.CreatedAt
            });
        }

        return Ok(new ResponseResult<List<ExamResponse>>
        {
            Success = true,
            Message = "Success",
            Data = exams.OrderByDescending(e => e.StartTime).ToList()
        });
    }

    /// <summary>
    /// Get available exams for student (currently active)
    /// </summary>
    [HttpGet("student/{studentId}/available")]
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetAvailableForStudent(long studentId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Student not found" });

        var classIds = (await _studentRepository.GetStudentClassesAsync(studentId)).Select(cs => cs.ClassId).Distinct().ToList();
        var attempted = (await _examAttemptRepository.GetStudentAttemptsAsync(studentId))
            .Where(a => a.Status is "SUBMITTED" or "GRADED")
            .Select(a => a.ExamId)
            .ToHashSet();

        var now = DateTime.UtcNow;
        var exams = new List<ExamResponse>();

        foreach (var classId in classIds)
        {
            var examClasses = await _examClassRepository.GetClassExamsAsync(classId);
            foreach (var examClass in examClasses)
            {
                if (attempted.Contains(examClass.ExamId))
                    continue;

                var exam = await _examRepository.GetByIdAsync(examClass.ExamId);
                if (exam == null || exam.Status != "ACTIVE" || exam.StartTime > now || exam.EndTime < now)
                    continue;

                exams.Add(new ExamResponse
                {
                    Id = exam.Id,
                    Title = exam.Title,
                    SubjectId = exam.SubjectId,
                    SubjectName = exam.Subject?.Name ?? string.Empty,
                    CreatedBy = exam.CreatedBy,
                    DurationMinutes = exam.DurationMinutes,
                    StartTime = exam.StartTime,
                    EndTime = exam.EndTime,
                    Description = exam.Description,
                    Status = exam.Status,
                    CreatedAt = exam.CreatedAt
                });
            }
        }

        return Ok(new ResponseResult<List<ExamResponse>>
        {
            Success = true,
            Message = "Success",
            Data = exams.GroupBy(e => e.Id).Select(g => g.First()).OrderBy(e => e.EndTime).ToList()
        });
    }

    /// <summary>
    /// Get upcoming exams for student
    /// </summary>
    [HttpGet("student/{studentId}/upcoming")]
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetUpcomingForStudent(long studentId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Student not found" });

        var classIds = (await _studentRepository.GetStudentClassesAsync(studentId)).Select(cs => cs.ClassId).Distinct().ToList();
        var now = DateTime.UtcNow;
        var exams = new List<ExamResponse>();

        foreach (var classId in classIds)
        {
            var examClasses = await _examClassRepository.GetClassExamsAsync(classId);
            foreach (var examClass in examClasses)
            {
                var exam = await _examRepository.GetByIdAsync(examClass.ExamId);
                if (exam == null || exam.Status != "ACTIVE" || exam.StartTime <= now)
                    continue;

                exams.Add(new ExamResponse
                {
                    Id = exam.Id,
                    Title = exam.Title,
                    SubjectId = exam.SubjectId,
                    SubjectName = exam.Subject?.Name ?? string.Empty,
                    CreatedBy = exam.CreatedBy,
                    DurationMinutes = exam.DurationMinutes,
                    StartTime = exam.StartTime,
                    EndTime = exam.EndTime,
                    Description = exam.Description,
                    Status = exam.Status,
                    CreatedAt = exam.CreatedAt
                });
            }
        }

        return Ok(new ResponseResult<List<ExamResponse>>
        {
            Success = true,
            Message = "Success",
            Data = exams.GroupBy(e => e.Id).Select(g => g.First()).OrderBy(e => e.StartTime).ToList()
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

    /// <summary>
    /// Configure exam settings
    /// </summary>
    [HttpPost("{examId}/settings")]
    public async Task<ActionResult<ResponseResult<ExamSettingsResponse>>> ConfigureSettings(long examId, [FromBody] ConfigureExamSettingsRequest request)
    {
        _logger.LogInformation("Configuring settings for exam: {ExamId}", examId);

        var (success, message, data) = await _examService.ConfigureSettingsAsync(examId, request);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<ExamSettingsResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get exam settings
    /// </summary>
    [HttpGet("{examId}/settings")]
    public async Task<ActionResult<ResponseResult<ExamSettingsResponse>>> GetSettings(long examId)
    {
        _logger.LogInformation("Getting settings for exam: {ExamId}", examId);

        var (success, message, data) = await _examService.GetSettingsAsync(examId);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<ExamSettingsResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Activate exam (transition from DRAFT to ACTIVE)
    /// </summary>
    [HttpPost("{examId}/activate")]
    public async Task<ActionResult<ResponseResult<ActivateExamResponse>>> ActivateExam(long examId)
    {
        _logger.LogInformation("Activating exam: {ExamId}", examId);

        var (success, message, data) = await _examService.ActivateExamAsync(examId);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<ActivateExamResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Close exam (transition from ACTIVE to CLOSED)
    /// </summary>
    [HttpPost("{examId}/close")]
    public async Task<ActionResult<ResponseResult<object>>> CloseExam(long examId)
    {
        _logger.LogInformation("Closing exam: {ExamId}", examId);

        var (success, message) = await _examService.CloseExamAsync(examId);

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

    /// <summary>
    /// Change exam status
    /// </summary>
    [HttpPost("{examId}/status")]
    public async Task<ActionResult<ResponseResult<object>>> ChangeStatus(long examId, [FromBody] ChangeExamStatusRequest request)
    {
        _logger.LogInformation("Changing exam status: {ExamId} to {Status}", examId, request.Status);

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Status is required"
            });
        }

        var (success, message) = await _examService.ChangeStatusAsync(examId, request.Status);

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

    /// <summary>
    /// Publish exam
    /// </summary>
    [HttpPost("{examId}/publish")]
    public async Task<ActionResult<ResponseResult<ActivateExamResponse>>> PublishExam(long examId)
    {
        _logger.LogInformation("Publishing exam: {ExamId}", examId);

        var (success, message, data) = await _examService.ActivateExamAsync(examId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<ActivateExamResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Unpublish exam (back to draft)
    /// </summary>
    [HttpPost("{examId}/unpublish")]
    public async Task<ActionResult<ResponseResult<object>>> UnpublishExam(long examId)
    {
        _logger.LogInformation("Unpublishing exam: {ExamId}", examId);

        var (success, message) = await _examService.ChangeStatusAsync(examId, "DRAFT");
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    /// <summary>
    /// Start exam (make active)
    /// </summary>
    [HttpPost("{examId}/start")]
    public async Task<ActionResult<ResponseResult<ActivateExamResponse>>> StartExam(long examId)
    {
        _logger.LogInformation("Starting exam: {ExamId}", examId);

        var (success, message, data) = await _examService.ActivateExamAsync(examId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<ActivateExamResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Stop exam (close)
    /// </summary>
    [HttpPost("{examId}/stop")]
    public async Task<ActionResult<ResponseResult<object>>> StopExam(long examId)
    {
        _logger.LogInformation("Stopping exam: {ExamId}", examId);

        var (success, message) = await _examService.CloseExamAsync(examId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    /// <summary>
    /// Duplicate exam
    /// </summary>
    [HttpPost("{examId}/duplicate")]
    public async Task<ActionResult<ResponseResult<DuplicateExamResponse>>> DuplicateExam(long examId)
    {
        _logger.LogInformation("Duplicating exam: {ExamId}", examId);

        var (getSuccess, getMessage, original) = await _examService.GetExamByIdAsync(examId);
        if (!getSuccess || original == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = getMessage });

        var createRequest = new CreateExamRequest
        {
            Title = $"{original.Title} (Copy)",
            SubjectId = original.SubjectId,
            CreatedBy = original.CreatedBy,
            DurationMinutes = original.DurationMinutes,
            StartTime = original.StartTime,
            EndTime = original.EndTime,
            Description = original.Description
        };

        var (success, message, newExam) = await _examService.CreateExamAsync(createRequest);
        if (!success || newExam == null)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<DuplicateExamResponse>
        {
            Success = true,
            Message = "Exam duplicated successfully",
            Data = new DuplicateExamResponse
            {
                OriginalExamId = examId,
                NewExamId = newExam.Id,
                Title = newExam.Title,
                Status = newExam.Status
            }
        });
    }

    /// <summary>
    /// Preview exam with questions
    /// </summary>
    [HttpGet("{examId}/preview")]
    public async Task<ActionResult<ResponseResult<ExamPreviewResponse>>> PreviewExam(long examId)
    {
        _logger.LogInformation("Previewing exam: {ExamId}", examId);

        var (examSuccess, examMessage, exam) = await _examService.GetExamByIdAsync(examId);
        if (!examSuccess || exam == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = examMessage });

        var (qSuccess, qMessage, questionsData) = await _examQuestionService.GetExamQuestionsAsync(examId);

        var preview = new ExamPreviewResponse
        {
            Id = exam.Id,
            Title = exam.Title,
            SubjectName = exam.SubjectName,
            DurationMinutes = exam.DurationMinutes,
            StartTime = exam.StartTime,
            EndTime = exam.EndTime,
            Description = exam.Description,
            Status = exam.Status,
            TotalQuestions = questionsData?.Questions?.Count ?? 0,
            Questions = questionsData?.Questions?.Select(q => new ExamPreviewQuestionResponse
            {
                QuestionId = q.QuestionId,
                Content = q.QuestionContent ?? string.Empty,
                QuestionType = q.QuestionDifficulty ?? string.Empty,
                OrderIndex = q.QuestionOrder,
                MaxScore = q.MaxScore
            }).ToList() ?? new()
        };

        return Ok(new ResponseResult<ExamPreviewResponse> { Success = true, Message = "Success", Data = preview });
    }

    /// <summary>
    /// Assign class to exam
    /// </summary>
    [HttpPost("{examId}/assign-class")]
    public async Task<ActionResult<ResponseResult<ExamClassResponse>>> AssignClass(long examId, [FromBody] AssignClassToExamRequest request)
    {
        _logger.LogInformation("Assigning class {ClassId} to exam {ExamId}", request.ClassId, examId);

        var (success, message, data) = await _examClassService.AssignClassToExamAsync(examId, request.ClassId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamClassResponse> { Success = true, Message = message, Data = data });
    }
}
