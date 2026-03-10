using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize]
[Produces("application/json")]
[Tags("Statistics")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    [HttpPost("exams/{examId}/calculate")]
    public async Task<ActionResult<ResponseResult<ExamStatisticResponse>>> Calculate(long examId)
    {
        var (success, message, data) = await _statisticsService.CalculateAndSaveExamStatisticsAsync(examId);
        if (!success)
            return BadRequest(new ResponseResult<ExamStatisticResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamStatisticResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet("exams/{examId}")]
    public async Task<ActionResult<ResponseResult<ExamStatisticResponse>>> GetExamStats(long examId)
    {
        var (success, message, data) = await _statisticsService.GetExamStatisticsAsync(examId);
        if (!success)
            return NotFound(new ResponseResult<ExamStatisticResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamStatisticResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet("exams/{examId}/distribution")]
    public async Task<ActionResult<ResponseResult<ScoreDistributionResponse>>> GetDistribution(long examId)
    {
        var (success, message, data) = await _statisticsService.GetScoreDistributionAsync(examId);
        if (!success)
            return NotFound(new ResponseResult<ScoreDistributionResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ScoreDistributionResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet("students/{studentId}/performance")]
    public async Task<ActionResult<ResponseResult<StudentPerformanceResponse>>> GetStudentPerformance(long studentId)
    {
        var (success, message, data) = await _statisticsService.GetStudentPerformanceAsync(studentId);
        if (!success)
            return NotFound(new ResponseResult<StudentPerformanceResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<StudentPerformanceResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet("classes/{classId}/results")]
    public async Task<ActionResult<ResponseResult<ClassResultsResponse>>> GetClassResults(long classId, [FromQuery] long examId)
    {
        var (success, message, data) = await _statisticsService.GetClassResultsAsync(classId, examId);
        if (!success)
            return NotFound(new ResponseResult<ClassResultsResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ClassResultsResponse> { Success = true, Message = message, Data = data });
    }

    [HttpGet("classes/{classId}/results/export")]
    [Authorize(Roles = "ADMIN,TEACHER")]
    public async Task<IActionResult> ExportClassResults(long classId, [FromQuery] long examId)
    {
        var (success, message, data) = await _statisticsService.GetClassResultsAsync(classId, examId);
        if (!success || data == null)
            return NotFound(new ResponseResult<object> { Success = false, Message = message });

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("ClassResults");

        worksheet.Cells[1, 1].Value = "Exam";
        worksheet.Cells[1, 2].Value = data.ExamTitle;
        worksheet.Cells[2, 1].Value = "Class";
        worksheet.Cells[2, 2].Value = data.ClassName;
        worksheet.Cells[3, 1].Value = "Average Score";
        worksheet.Cells[3, 2].Value = data.AverageScore;
        worksheet.Cells[4, 1].Value = "Attempted";
        worksheet.Cells[4, 2].Value = data.AttemptedCount;
        worksheet.Cells[5, 1].Value = "Total Students";
        worksheet.Cells[5, 2].Value = data.TotalStudents;

        worksheet.Cells[6, 1].Value = "Attempt Id";
        worksheet.Cells[6, 2].Value = "Student Name";
        worksheet.Cells[6, 3].Value = "Score";
        worksheet.Cells[6, 4].Value = "Status";

        var row = 7;
        foreach (var student in data.StudentResults)
        {
            worksheet.Cells[row, 1].Value = student.AttemptId;
            worksheet.Cells[row, 2].Value = student.StudentName;
            worksheet.Cells[row, 3].Value = student.Score;
            worksheet.Cells[row, 4].Value = student.Status;
            row++;
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        var bytes = await package.GetAsByteArrayAsync();
        var fileName = $"class_{classId}_exam_{examId}_results.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
