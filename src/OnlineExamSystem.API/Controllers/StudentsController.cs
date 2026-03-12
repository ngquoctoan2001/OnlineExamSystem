namespace OnlineExamSystem.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Student management API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Students")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly IStatisticsService _statisticsService;
    private readonly IExamAttemptService _examAttemptService;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(
        IStudentService studentService,
        IStatisticsService statisticsService,
        IExamAttemptService examAttemptService,
        ILogger<StudentsController> logger)
    {
        _studentService = studentService;
        _statisticsService = statisticsService;
        _examAttemptService = examAttemptService;
        _logger = logger;
    }

    /// <summary>
    /// Get all students
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<StudentListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting all students: page={Page}, pageSize={PageSize}", page, pageSize);
        
        var (success, message, data) = await _studentService.GetAllStudentsAsync(page, pageSize);
        
        return Ok(new ResponseResult<StudentListResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get current student profile (by authenticated user)
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = "STUDENT")]
    public async Task<ActionResult<ResponseResult<StudentResponse>>> GetMe()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ResponseResult<object> { Success = false, Message = "Invalid token" });
        }

        var (success, message, data) = await _studentService.GetStudentByUserIdAsync(userId);
        if (!success)
        {
            return NotFound(new ResponseResult<object> { Success = false, Message = message });
        }

        return Ok(new ResponseResult<StudentResponse> { Success = success, Message = message, Data = data });
    }

    /// <summary>
    /// Get student by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<StudentResponse>>> GetById(long id)
    {
        _logger.LogInformation("Getting student: {StudentId}", id);
        
        var (success, message, data) = await _studentService.GetStudentByIdAsync(id);
        
        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<StudentResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Search students by name or student code
    /// </summary>
    [HttpGet("search/{searchTerm}")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<List<StudentResponse>>>> Search(string searchTerm)
    {
        _logger.LogInformation("Searching students: {SearchTerm}", searchTerm);
        
        var (success, message, data) = await _studentService.SearchStudentsAsync(searchTerm);
        
        return Ok(new ResponseResult<List<StudentResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Create new student
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<StudentResponse>>> Create([FromBody] CreateStudentRequest request)
    {
        _logger.LogInformation("Creating new student: {Username}", request.Username);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _studentService.CreateStudentAsync(request);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = data?.Id }, new ResponseResult<StudentResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Update student information
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<StudentResponse>>> Update(long id, [FromBody] UpdateStudentRequest request)
    {
        _logger.LogInformation("Updating student: {StudentId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _studentService.UpdateStudentAsync(id, request);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<StudentResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Delete student
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        _logger.LogInformation("Deleting student: {StudentId}", id);

        var (success, message) = await _studentService.DeleteStudentAsync(id);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
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
    /// Get classes enrolled by student
    /// </summary>
    [HttpGet("{id}/classes")]
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    public async Task<ActionResult<ResponseResult<List<StudentClassEnrollmentResponse>>>> GetStudentClasses(long id)
    {
        _logger.LogInformation("Getting classes for student: {StudentId}", id);

        var (success, message, data) = await _studentService.GetStudentClassesAsync(id);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<List<StudentClassEnrollmentResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Export all students to Excel file
    /// </summary>
    [HttpGet("export")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Export()
    {
        var (success, _, data) = await _studentService.GetAllStudentsAsync(1, 10000);
        if (!success || data == null)
            return BadRequest("Failed to get students");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Students");

        ws.Cells[1, 1].Value = "StudentCode";
        ws.Cells[1, 2].Value = "FullName";
        ws.Cells[1, 3].Value = "Username";
        ws.Cells[1, 4].Value = "Email";
        ws.Cells[1, 5].Value = "RollNumber";
        ws.Cells[1, 6].Value = "Status";

        using (var range = ws.Cells[1, 1, 1, 6])
        {
            range.Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var s in data.Students)
        {
            ws.Cells[row, 1].Value = s.StudentCode;
            ws.Cells[row, 2].Value = s.FullName;
            ws.Cells[row, 3].Value = s.Username;
            ws.Cells[row, 4].Value = s.Email;
            ws.Cells[row, 5].Value = s.RollNumber;
            ws.Cells[row, 6].Value = s.IsActive ? "Active" : "Inactive";
            row++;
        }

        ws.Cells.AutoFitColumns();
        var bytes = package.GetAsByteArray();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "students.xlsx");
    }

    /// <summary>
    /// Get student scores
    /// </summary>
    [HttpGet("{id}/scores")]
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    public async Task<ActionResult<ResponseResult<StudentPerformanceResponse>>> GetStudentScores(long id)
    {
        _logger.LogInformation("Getting scores for student: {StudentId}", id);

        var (success, message, data) = await _statisticsService.GetStudentPerformanceAsync(id);
        if (!success)
            return NotFound(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<StudentPerformanceResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Get student exams
    /// </summary>
    [HttpGet("{id}/exams")]
    [Authorize(Roles = "ADMIN,TEACHER,STUDENT")]
    public async Task<ActionResult<ResponseResult<List<ExamAttemptResponse>>>> GetStudentExams(long id)
    {
        _logger.LogInformation("Getting exams for student: {StudentId}", id);

        var (success, message, data) = await _examAttemptService.GetStudentAttemptsAsync(id);

        return Ok(new ResponseResult<List<ExamAttemptResponse>> { Success = success, Message = message, Data = data });
    }
}
