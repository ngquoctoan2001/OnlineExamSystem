namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;
using System.Security.Claims;
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
    private readonly ITeacherRepository _teacherRepository;
    private readonly ITeachingAssignmentRepository _teachingAssignmentRepository;
    private readonly ILogger<ExamsController> _logger;

    public ExamsController(
        IExamService examService,
        IExamRepository examRepository,
        IExamClassRepository examClassRepository,
        IExamClassService examClassService,
        IExamQuestionService examQuestionService,
        IStudentRepository studentRepository,
        IExamAttemptRepository examAttemptRepository,
        ITeacherRepository teacherRepository,
        ITeachingAssignmentRepository teachingAssignmentRepository,
        ILogger<ExamsController> logger)
    {
        _examService = examService;
        _examRepository = examRepository;
        _examClassRepository = examClassRepository;
        _examClassService = examClassService;
        _examQuestionService = examQuestionService;
        _studentRepository = studentRepository;
        _examAttemptRepository = examAttemptRepository;
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

    private async Task<bool> TeacherCanTeachSubjectAsync(long teacherId, long subjectId)
    {
        var assignments = await _teachingAssignmentRepository.GetByTeacherAsync(teacherId);
        return assignments.Any(a => a.SubjectId == subjectId);
    }

    private async Task<bool> CurrentTeacherOwnsExamAsync(long examId)
    {
        if (User.IsInRole("ADMIN"))
            return true;

        if (!User.IsInRole("TEACHER"))
            return false;

        var teacher = await GetCurrentTeacherAsync();
        if (teacher == null)
            return false;

        var exam = await _examRepository.GetByIdAsync(examId);
        return exam != null && exam.CreatedBy == teacher.Id;
    }

    /// <summary>
    /// Get all exams (filtered by user role: Admin sees all, Teachers see only their own, Students see assigned)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseResult<ExamListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting all exams: page={Page}, pageSize={PageSize}", page, pageSize);
        
        // For ADMIN: show all exams
        if (User.IsInRole("ADMIN"))
        {
            var (success, message, data) = await _examService.GetAllExamsAsync(page, pageSize);
            return Ok(new ResponseResult<ExamListResponse>
            {
                Success = success,
                Message = message,
                Data = data
            });
        }

        // For TEACHER: show only their own exams
        if (User.IsInRole("TEACHER"))
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return Unauthorized(new ResponseResult<ExamListResponse>
                {
                    Success = false,
                    Message = "Teacher profile not found"
                });
            }

            var (success, message, exams) = await _examService.GetExamsByTeacherAsync(teacher.Id);
            if (!success)
            {
                return BadRequest(new ResponseResult<ExamListResponse>
                {
                    Success = false,
                    Message = message
                });
            }

            // Apply pagination
            var totalCount = exams?.Count ?? 0;
            var skip = (page - 1) * pageSize;
            var paginatedExams = exams?.Skip(skip).Take(pageSize).ToList() ?? [];

            var response = new ExamListResponse
            {
                Items = paginatedExams,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(new ResponseResult<ExamListResponse>
            {
                Success = true,
                Message = "Success",
                Data = response
            });
        }

        // For STUDENT: show only exams assigned to their classes
        if (User.IsInRole("STUDENT"))
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ResponseResult<ExamListResponse>
                {
                    Success = false,
                    Message = "Student not found"
                });
            }

            var student = await _studentRepository.GetByUserIdAsync(userId.Value);
            if (student == null)
            {
                return Unauthorized(new ResponseResult<ExamListResponse>
                {
                    Success = false,
                    Message = "Student profile not found"
                });
            }

            var classIds = (await _studentRepository.GetStudentClassesAsync(student.Id))
                .Select(cs => cs.ClassId)
                .Distinct()
                .ToList();

            var allExams = new List<ExamResponse>();
            foreach (var classId in classIds)
            {
                var examClasses = await _examClassRepository.GetClassExamsAsync(classId);
                foreach (var examClass in examClasses)
                {
                    var exam = await _examRepository.GetByIdAsync(examClass.ExamId);
                    if (exam != null)
                    {
                        allExams.Add(new ExamResponse
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
            }

            // Remove duplicates and apply pagination
            var distinctExams = allExams.DistinctBy(e => e.Id).OrderByDescending(e => e.StartTime).ToList();
            var totalCount = distinctExams.Count;
            var skip = (page - 1) * pageSize;
            var paginatedExams = distinctExams.Skip(skip).Take(pageSize).ToList();

            var response = new ExamListResponse
            {
                Items = paginatedExams,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return Ok(new ResponseResult<ExamListResponse>
            {
                Success = true,
                Message = "Success",
                Data = response
            });
        }

        return Unauthorized(new ResponseResult<ExamListResponse>
        {
            Success = false,
            Message = "Unauthorized"
        });
    }

    /// <summary>
    /// Get exam by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseResult<ExamResponse>>> GetById(long id)
    {
        _logger.LogInformation("Getting exam: {ExamId}", id);
        
        // Check authorization for TEACHER role
        if (User.IsInRole("TEACHER"))
        {
            var canAccess = await CurrentTeacherOwnsExamAsync(id);
            if (!canAccess)
            {
                return Forbid();
            }
        }

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
        
        // For ADMIN: search all exams
        if (User.IsInRole("ADMIN"))
        {
            var (success, message, data) = await _examService.SearchExamsAsync(searchTerm);
            return Ok(new ResponseResult<List<ExamResponse>>
            {
                Success = success,
                Message = message,
                Data = data
            });
        }

        // For TEACHER: search only their own exams
        if (User.IsInRole("TEACHER"))
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return Unauthorized(new ResponseResult<List<ExamResponse>>
                {
                    Success = false,
                    Message = "Teacher profile not found"
                });
            }

            var (success, message, allExams) = await _examService.GetExamsByTeacherAsync(teacher.Id);
            if (!success)
            {
                return Ok(new ResponseResult<List<ExamResponse>>
                {
                    Success = true,
                    Message = "No exams found",
                    Data = new List<ExamResponse>()
                });
            }

            // Filter search results by title or description
            var filtered = allExams?
                .Where(e => e.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<ExamResponse>();

            return Ok(new ResponseResult<List<ExamResponse>>
            {
                Success = true,
                Message = "Success",
                Data = filtered
            });
        }

        // For STUDENT: search exams in their classes
        if (User.IsInRole("STUDENT"))
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ResponseResult<List<ExamResponse>>
                {
                    Success = false,
                    Message = "Student not found"
                });
            }

            var student = await _studentRepository.GetByUserIdAsync(userId.Value);
            if (student == null)
            {
                return Unauthorized(new ResponseResult<List<ExamResponse>>
                {
                    Success = false,
                    Message = "Student profile not found"
                });
            }

            var classIds = (await _studentRepository.GetStudentClassesAsync(student.Id))
                .Select(cs => cs.ClassId)
                .Distinct()
                .ToList();

            var allExams = new List<ExamResponse>();
            foreach (var classId in classIds)
            {
                var examClasses = await _examClassRepository.GetClassExamsAsync(classId);
                foreach (var examClass in examClasses)
                {
                    var exam = await _examRepository.GetByIdAsync(examClass.ExamId);
                    if (exam != null)
                    {
                        allExams.Add(new ExamResponse
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
            }

            // Filter search results
            var filtered = allExams
                .Where(e => e.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Ok(new ResponseResult<List<ExamResponse>>
            {
                Success = true,
                Message = "Success",
                Data = filtered
            });
        }

        return Unauthorized(new ResponseResult<List<ExamResponse>>
        {
            Success = false,
            Message = "Unauthorized"
        });
    }

    /// <summary>
    /// Get exams by teacher
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpGet("teacher/{teacherId}")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetByTeacher(long teacherId)
    {
        _logger.LogInformation("Getting exams by teacher: {TeacherId}", teacherId);
        
        // TEACHER can only access their own exams
        if (User.IsInRole("TEACHER"))
        {
            var currentTeacher = await GetCurrentTeacherAsync();
            if (currentTeacher == null || currentTeacher.Id != teacherId)
            {
                return Forbid();
            }
        }
        
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
            Success = true,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get exams by subject
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpGet("subject/{subjectId}")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetBySubject(long subjectId)
    {
        _logger.LogInformation("Getting exams by subject: {SubjectId}", subjectId);
        
        // For ADMIN: return all exams for the subject
        if (User.IsInRole("ADMIN"))
        {
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
                Success = true,
                Message = message,
                Data = data
            });
        }

        // For TEACHER: return only their exams for the subject
        if (User.IsInRole("TEACHER"))
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return Unauthorized(new ResponseResult<List<ExamResponse>>
                {
                    Success = false,
                    Message = "Teacher profile not found"
                });
            }

            var (success, message, allExams) = await _examService.GetExamsByTeacherAsync(teacher.Id);
            if (!success || allExams == null)
            {
                return Ok(new ResponseResult<List<ExamResponse>>
                {
                    Success = true,
                    Message = "Success",
                    Data = new List<ExamResponse>()
                });
            }

            // Filter by subject
            var filtered = allExams.Where(e => e.SubjectId == subjectId).ToList();
            return Ok(new ResponseResult<List<ExamResponse>>
            {
                Success = true,
                Message = "Success",
                Data = filtered
            });
        }

        return Unauthorized(new ResponseResult<List<ExamResponse>>
        {
            Success = false,
            Message = "Unauthorized"
        });
    }

    /// <summary>
    /// Get exams assigned to a class
    /// </summary>
    [HttpGet("class/{classId}")]
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetByClass(long classId)
    {
        // For TEACHER: verify they teach this class
        if (User.IsInRole("TEACHER"))
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return Forbid();
            }

            var assignments = await _teachingAssignmentRepository.GetByClassAsync(classId);
            if (!assignments.Any(a => a.TeacherId == teacher.Id))
            {
                return Forbid();
            }
        }

        // For STUDENT: verify they are enrolled in this class
        if (User.IsInRole("STUDENT"))
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Forbid();
            }

            var student = await _studentRepository.GetByUserIdAsync(userId.Value);
            if (student == null)
            {
                return Forbid();
            }

            var studentClasses = await _studentRepository.GetStudentClassesAsync(student.Id);
            if (!studentClasses.Any(cs => cs.ClassId == classId))
            {
                return Forbid();
            }
        }

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
    /// Get available exams for student (currently active and within time window)
    /// </summary>
    [HttpGet("student/{studentId}/available")]
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetAvailableForStudent(long studentId)
    {
        // STUDENT can only access their own available exams
        if (User.IsInRole("STUDENT"))
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Forbid();
            }

            var currentStudent = await _studentRepository.GetByUserIdAsync(userId.Value);
            if (currentStudent == null || currentStudent.Id != studentId)
            {
                return Forbid();
            }
        }

        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Student not found" });

        var classIds = (await _studentRepository.GetStudentClassesAsync(studentId)).Select(cs => cs.ClassId).Distinct().ToList();
        _logger.LogInformation("Student {StudentId} belongs to {ClassCount} classes: [{ClassIds}]", studentId, classIds.Count, string.Join(", ", classIds));

        var attempted = (await _examAttemptRepository.GetStudentAttemptsAsync(studentId))
            .Where(a => a.Status is "SUBMITTED" or "GRADED")
            .Select(a => a.ExamId)
            .ToHashSet();

        var now = DateTime.UtcNow;
        var exams = new List<ExamResponse>();

        foreach (var classId in classIds)
        {
            var examClasses = await _examClassRepository.GetClassExamsAsync(classId);
            _logger.LogInformation("Class {ClassId} has {ExamCount} assigned exams", classId, examClasses.Count);

            foreach (var examClass in examClasses)
            {
                if (attempted.Contains(examClass.ExamId))
                    continue;

                var exam = await _examRepository.GetByIdAsync(examClass.ExamId);
                if (exam == null)
                    continue;

                // Show ACTIVE or DRAFT exams that are within the time window
                var isActiveOrDraft = exam.Status is "ACTIVE" or "DRAFT";
                var inTimeWindow = exam.StartTime <= now && exam.EndTime >= now;

                _logger.LogInformation("Exam {ExamId} '{Title}': Status={Status}, StartTime={Start}, EndTime={End}, Now={Now}, InTimeWindow={InWindow}",
                    exam.Id, exam.Title, exam.Status, exam.StartTime, exam.EndTime, now, inTimeWindow);

                if (!(isActiveOrDraft && inTimeWindow))
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

        _logger.LogInformation("Returning {Count} available exams for student {StudentId}", exams.Count, studentId);
        return Ok(new ResponseResult<List<ExamResponse>>
        {
            Success = true,
            Message = "Success",
            Data = exams.GroupBy(e => e.Id).Select(g => g.First()).OrderBy(e => e.EndTime).ToList()
        });
    }

    /// <summary>
    /// Get upcoming exams for student (not yet started, including DRAFT)
    /// </summary>
    [HttpGet("student/{studentId}/upcoming")]
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    public async Task<ActionResult<ResponseResult<List<ExamResponse>>>> GetUpcomingForStudent(long studentId)
    {
        // STUDENT can only access their own upcoming exams
        if (User.IsInRole("STUDENT"))
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Forbid();
            }

            var currentStudent = await _studentRepository.GetByUserIdAsync(userId.Value);
            if (currentStudent == null || currentStudent.Id != studentId)
            {
                return Forbid();
            }
        }

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
                if (exam == null)
                    continue;

                // Show upcoming exams: ACTIVE or DRAFT, not yet started or not yet ended
                var isRelevantStatus = exam.Status is "ACTIVE" or "DRAFT";
                var isUpcoming = exam.StartTime > now;
                var isOngoing = exam.StartTime <= now && exam.EndTime >= now && exam.Status == "DRAFT";

                if (!isRelevantStatus || (!isUpcoming && !isOngoing))
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

        _logger.LogInformation("Returning {Count} upcoming exams for student {StudentId}", exams.Count, studentId);
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
    [Authorize(Roles = "ADMIN,TEACHER")]
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

        if (User.IsInRole("TEACHER") && !User.IsInRole("ADMIN"))
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
                return Forbid();

            // Prevent request spoofing: teacher can only create with own identity.
            request.CreatedBy = teacher.Id;

            var canTeachSubject = await TeacherCanTeachSubjectAsync(teacher.Id, request.SubjectId);
            if (!canTeachSubject)
                return Forbid();
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
    [Authorize(Roles = "ADMIN,TEACHER")]
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

        var (getSuccess, _, existingExam) = await _examService.GetExamByIdAsync(id);
        if (!getSuccess || existingExam == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Exam not found" });

        if (User.IsInRole("TEACHER") && !User.IsInRole("ADMIN"))
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
                return Forbid();

            if (existingExam.CreatedBy != teacher.Id)
                return Forbid();

            var canTeachSubject = await TeacherCanTeachSubjectAsync(teacher.Id, request.SubjectId);
            if (!canTeachSubject)
                return Forbid();
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
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        _logger.LogInformation("Deleting exam: {ExamId}", id);

        var (getSuccess, _, existingExam) = await _examService.GetExamByIdAsync(id);
        if (!getSuccess || existingExam == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Exam not found" });

        if (User.IsInRole("TEACHER") && !User.IsInRole("ADMIN"))
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null || existingExam.CreatedBy != teacher.Id)
                return Forbid();
        }

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
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{examId}/settings")]
    public async Task<ActionResult<ResponseResult<ExamSettingsResponse>>> ConfigureSettings(long examId, [FromBody] ConfigureExamSettingsRequest request)
    {
        _logger.LogInformation("Configuring settings for exam: {ExamId}", examId);

        if (!await CurrentTeacherOwnsExamAsync(examId))
            return Forbid();

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
    [Authorize(Roles = "ADMIN,TEACHER")]
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
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{examId}/activate")]
    public async Task<ActionResult<ResponseResult<ActivateExamResponse>>> ActivateExam(long examId)
    {
        _logger.LogInformation("Activating exam: {ExamId}", examId);

        if (!await CurrentTeacherOwnsExamAsync(examId))
            return Forbid();

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
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{examId}/close")]
    public async Task<ActionResult<ResponseResult<object>>> CloseExam(long examId)
    {
        _logger.LogInformation("Closing exam: {ExamId}", examId);

        if (!await CurrentTeacherOwnsExamAsync(examId))
            return Forbid();

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
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{examId}/status")]
    public async Task<ActionResult<ResponseResult<object>>> ChangeStatus(long examId, [FromBody] ChangeExamStatusRequest request)
    {
        _logger.LogInformation("Changing exam status: {ExamId} to {Status}", examId, request.Status);

        if (!await CurrentTeacherOwnsExamAsync(examId))
            return Forbid();

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
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{examId}/publish")]
    public async Task<ActionResult<ResponseResult<ActivateExamResponse>>> PublishExam(long examId)
    {
        _logger.LogInformation("Publishing exam: {ExamId}", examId);

        if (!await CurrentTeacherOwnsExamAsync(examId))
            return Forbid();

        var (success, message, data) = await _examService.ActivateExamAsync(examId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<ActivateExamResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Unpublish exam (back to draft)
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{examId}/unpublish")]
    public async Task<ActionResult<ResponseResult<object>>> UnpublishExam(long examId)
    {
        _logger.LogInformation("Unpublishing exam: {ExamId}", examId);

        if (!await CurrentTeacherOwnsExamAsync(examId))
            return Forbid();

        var (success, message) = await _examService.ChangeStatusAsync(examId, "DRAFT");
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    /// <summary>
    /// Start exam (make active)
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{examId}/start")]
    public async Task<ActionResult<ResponseResult<ActivateExamResponse>>> StartExam(long examId)
    {
        _logger.LogInformation("Starting exam: {ExamId}", examId);

        if (!await CurrentTeacherOwnsExamAsync(examId))
            return Forbid();

        var (success, message, data) = await _examService.ActivateExamAsync(examId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<ActivateExamResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Stop exam (close)
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{examId}/stop")]
    public async Task<ActionResult<ResponseResult<object>>> StopExam(long examId)
    {
        _logger.LogInformation("Stopping exam: {ExamId}", examId);

        if (!await CurrentTeacherOwnsExamAsync(examId))
            return Forbid();

        var (success, message) = await _examService.CloseExamAsync(examId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<object> { Success = true, Message = message });
    }

    /// <summary>
    /// Duplicate exam
    /// </summary>
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{examId}/duplicate")]
    public async Task<ActionResult<ResponseResult<DuplicateExamResponse>>> DuplicateExam(long examId)
    {
        _logger.LogInformation("Duplicating exam: {ExamId}", examId);

        if (!await CurrentTeacherOwnsExamAsync(examId))
            return Forbid();

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
    [Authorize(Roles = "ADMIN,TEACHER")]
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
    [Authorize(Roles = "ADMIN,TEACHER")]
    [HttpPost("{examId}/assign-class")]
    public async Task<ActionResult<ResponseResult<ExamClassResponse>>> AssignClass(long examId, [FromBody] AssignClassToExamRequest request)
    {
        _logger.LogInformation("Assigning class {ClassId} to exam {ExamId}", request.ClassId, examId);

        var exam = await _examRepository.GetByIdAsync(examId);
        if (exam == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = "Exam not found" });

        if (User.IsInRole("TEACHER") && !User.IsInRole("ADMIN"))
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
                return Forbid();

            // Teacher must be exam owner and assigned to this class-subject.
            if (exam.CreatedBy != teacher.Id)
                return Forbid();

            var assignment = await _teachingAssignmentRepository
                .GetByClassTeacherSubjectAsync(request.ClassId, teacher.Id, exam.SubjectId);
            if (assignment == null)
                return Forbid();
        }

        var (success, message, data) = await _examClassService.AssignClassToExamAsync(examId, request.ClassId);
        if (!success)
            return BadRequest(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamClassResponse> { Success = true, Message = message, Data = data });
    }
}
