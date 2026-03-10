namespace OnlineExamSystem.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Teacher management API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[Tags("Teachers")]
public class TeachersController : ControllerBase
{
    private readonly ITeacherService _teacherService;
    private readonly ILogger<TeachersController> _logger;

    public TeachersController(ITeacherService teacherService, ILogger<TeachersController> logger)
    {
        _teacherService = teacherService;
        _logger = logger;
    }

    /// <summary>
    /// Get all teachers
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<TeacherListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting all teachers: page={Page}, pageSize={PageSize}", page, pageSize);
        
        var (success, message, data) = await _teacherService.GetAllTeachersAsync(page, pageSize);
        
        return Ok(new ResponseResult<TeacherListResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Get teacher by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<TeacherResponse>>> GetById(long id)
    {
        _logger.LogInformation("Getting teacher: {TeacherId}", id);
        
        var (success, message, data) = await _teacherService.GetTeacherByIdAsync(id);
        
        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<TeacherResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Search teachers by name or employee ID
    /// </summary>
    [HttpGet("search/{searchTerm}")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<List<TeacherResponse>>>> Search(string searchTerm)
    {
        _logger.LogInformation("Searching teachers: {SearchTerm}", searchTerm);
        
        var (success, message, data) = await _teacherService.SearchTeachersAsync(searchTerm);
        
        return Ok(new ResponseResult<List<TeacherResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Create new teacher
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<TeacherResponse>>> Create([FromBody] CreateTeacherRequest request)
    {
        _logger.LogInformation("Creating new teacher: {Username}", request.Username);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _teacherService.CreateTeacherAsync(request);

        if (!success)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = data?.Id }, new ResponseResult<TeacherResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Update teacher information
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<TeacherResponse>>> Update(long id, [FromBody] UpdateTeacherRequest request)
    {
        _logger.LogInformation("Updating teacher: {TeacherId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ResponseResult<object>
            {
                Success = false,
                Message = "Invalid request"
            });
        }

        var (success, message, data) = await _teacherService.UpdateTeacherAsync(id, request);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<TeacherResponse>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Delete teacher
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ResponseResult<object>>> Delete(long id)
    {
        _logger.LogInformation("Deleting teacher: {TeacherId}", id);

        var (success, message) = await _teacherService.DeleteTeacherAsync(id);

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
    /// Get classes assigned to a teacher
    /// </summary>
    [HttpGet("{id}/classes")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<ActionResult<ResponseResult<List<TeacherClassAssignmentResponse>>>> GetTeacherClasses(long id)
    {
        _logger.LogInformation("Getting classes for teacher: {TeacherId}", id);

        var (success, message, data) = await _teacherService.GetTeacherClassesAsync(id);

        if (!success)
        {
            return NotFound(new ResponseResult<object>
            {
                Success = false,
                Message = message
            });
        }

        return Ok(new ResponseResult<List<TeacherClassAssignmentResponse>>
        {
            Success = success,
            Message = message,
            Data = data
        });
    }

    /// <summary>
    /// Export all teachers to Excel file
    /// </summary>
    [HttpGet("export")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Export()
    {
        var (success, _, data) = await _teacherService.GetAllTeachersAsync(1, 10000);
        if (!success || data == null)
            return BadRequest("Failed to get teachers");

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Teachers");

        ws.Cells[1, 1].Value = "EmployeeCode";
        ws.Cells[1, 2].Value = "FullName";
        ws.Cells[1, 3].Value = "Username";
        ws.Cells[1, 4].Value = "Email";
        ws.Cells[1, 5].Value = "Department";
        ws.Cells[1, 6].Value = "Status";

        using (var range = ws.Cells[1, 1, 1, 6])
        {
            range.Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var t in data.Teachers)
        {
            ws.Cells[row, 1].Value = t.EmployeeId;
            ws.Cells[row, 2].Value = t.FullName;
            ws.Cells[row, 3].Value = t.Username;
            ws.Cells[row, 4].Value = t.Email;
            ws.Cells[row, 5].Value = t.Department;
            ws.Cells[row, 6].Value = t.IsActive ? "Active" : "Inactive";
            row++;
        }

        ws.Cells.AutoFitColumns();
        var bytes = package.GetAsByteArray();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "teachers.xlsx");
    }
}
