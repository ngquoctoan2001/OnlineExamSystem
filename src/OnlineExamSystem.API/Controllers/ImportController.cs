using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/import")]
[Authorize]
[Produces("application/json")]
[Tags("Import")]
public class ImportController : ControllerBase
{
    private readonly IImportService _importService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(IImportService importService, ILogger<ImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Import teachers from Excel file.
    /// Expected columns: EmployeeCode, FirstName, LastName, Email, PhoneNumber, Department
    /// </summary>
    [HttpPost("teachers")]
    public async Task<ActionResult<ResponseResult<ImportResult>>> ImportTeachers(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "No file uploaded" });

        if (!IsExcelFile(file.FileName))
            return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "Only .xlsx and .xls files are supported" });

        var userId = long.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        using var stream = file.OpenReadStream();
        var (success, result) = await _importService.ImportTeachersAsync(stream, userId);

        return Ok(new ResponseResult<ImportResult> { Success = success, Message = success ? "Import completed" : "Import completed with errors", Data = result });
    }

    /// <summary>
    /// Import students from Excel file.
    /// Expected columns: StudentCode, FirstName, LastName, Email, PhoneNumber, ClassName, DateOfBirth
    /// </summary>
    [HttpPost("students")]
    public async Task<ActionResult<ResponseResult<ImportResult>>> ImportStudents(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "No file uploaded" });

        if (!IsExcelFile(file.FileName))
            return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "Only .xlsx and .xls files are supported" });

        var userId = long.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        using var stream = file.OpenReadStream();
        var (success, result) = await _importService.ImportStudentsAsync(stream, userId);

        return Ok(new ResponseResult<ImportResult> { Success = success, Message = success ? "Import completed" : "Import completed with errors", Data = result });
    }

    /// <summary>
    /// Import questions from Excel file.
    /// Expected columns: Content, QuestionType, Subject, Difficulty, OptionA, OptionB, OptionC, OptionD, CorrectOption
    /// </summary>
    [HttpPost("questions")]
    public async Task<ActionResult<ResponseResult<ImportResult>>> ImportQuestions(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "No file uploaded" });

        if (!IsExcelFile(file.FileName))
            return BadRequest(new ResponseResult<ImportResult> { Success = false, Message = "Only .xlsx and .xls files are supported" });

        var userId = long.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        using var stream = file.OpenReadStream();
        var (success, result) = await _importService.ImportQuestionsAsync(stream, userId);

        return Ok(new ResponseResult<ImportResult> { Success = success, Message = success ? "Import completed" : "Import completed with errors", Data = result });
    }

    private static bool IsExcelFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".xlsx" or ".xls";
    }
}
