using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.DTOs.Common;
using OnlineExamSystem.Infrastructure.Repositories;
using OnlineExamSystem.Infrastructure.Services;

namespace OnlineExamSystem.API.Controllers;

[ApiController]
[Route("api/scores")]
[Authorize]
[Produces("application/json")]
[Tags("Scores")]
public class ScoresController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly IExamAttemptRepository _examAttemptRepository;
    private readonly IExamRepository _examRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<ScoresController> _logger;

    public ScoresController(
        IStatisticsService statisticsService,
        IExamAttemptRepository examAttemptRepository,
        IExamRepository examRepository,
        IStudentRepository studentRepository,
        ILogger<ScoresController> logger)
    {
        _statisticsService = statisticsService;
        _examAttemptRepository = examAttemptRepository;
        _examRepository = examRepository;
        _studentRepository = studentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get scores for a student
    /// </summary>
    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<ResponseResult<StudentPerformanceResponse>>> GetStudentScores(long studentId)
    {
        _logger.LogInformation("Getting scores for student: {StudentId}", studentId);

        var (success, message, data) = await _statisticsService.GetStudentPerformanceAsync(studentId);
        if (!success)
            return NotFound(new ResponseResult<object> { Success = false, Message = message });

        return Ok(new ResponseResult<StudentPerformanceResponse> { Success = true, Message = message, Data = data });
    }

    /// <summary>
    /// Get scores for a class
    /// </summary>
    [HttpGet("class/{classId}")]
    public async Task<ActionResult<ResponseResult<List<ScoreResponse>>>> GetClassScores(long classId, [FromQuery] long? examId = null)
    {
        _logger.LogInformation("Getting scores for class: {ClassId}", classId);

        if (examId.HasValue)
        {
            var (success, message, data) = await _statisticsService.GetClassResultsAsync(classId, examId.Value);
            if (!success)
                return NotFound(new ResponseResult<object> { Success = false, Message = message });

            var scores = data?.StudentResults?.Select(s => new ScoreResponse
            {
                AttemptId = s.AttemptId,
                ExamId = s.ExamId,
                ExamTitle = s.ExamTitle,
                StudentId = 0,
                StudentName = s.StudentName,
                Score = s.Score,
                Status = s.Status,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList() ?? new();

            return Ok(new ResponseResult<List<ScoreResponse>> { Success = true, Message = "Success", Data = scores });
        }

        return Ok(new ResponseResult<List<ScoreResponse>>
        {
            Success = true,
            Message = "Provide examId query parameter for class scores",
            Data = new List<ScoreResponse>()
        });
    }

    /// <summary>
    /// Get scores for an exam
    /// </summary>
    [HttpGet("exam/{examId}")]
    public async Task<ActionResult<ResponseResult<List<ScoreResponse>>>> GetExamScores(long examId)
    {
        _logger.LogInformation("Getting scores for exam: {ExamId}", examId);

        var attempts = await _examAttemptRepository.GetExamAttemptsAsync(examId);
        var exam = await _examRepository.GetByIdAsync(examId);

        var scores = attempts.Select(a => new ScoreResponse
        {
            AttemptId = a.Id,
            ExamId = a.ExamId,
            ExamTitle = exam?.Title ?? string.Empty,
            StudentId = a.StudentId,
            StudentName = a.Student?.User?.FullName ?? string.Empty,
            Score = a.Score,
            Status = a.Status,
            StartTime = a.StartTime,
            EndTime = a.EndTime
        }).ToList();

        return Ok(new ResponseResult<List<ScoreResponse>> { Success = true, Message = "Success", Data = scores });
    }

    /// <summary>
    /// Get exam ranking
    /// </summary>
    [HttpGet("exam/{examId}/ranking")]
    public async Task<ActionResult<ResponseResult<ExamRankingResponse>>> GetExamRanking(long examId)
    {
        _logger.LogInformation("Getting ranking for exam: {ExamId}", examId);

        var attempts = await _examAttemptRepository.GetExamAttemptsAsync(examId);
        var exam = await _examRepository.GetByIdAsync(examId);

        var rankings = attempts
            .Where(a => a.Status is "SUBMITTED" or "GRADED")
            .OrderByDescending(a => a.Score)
            .Select((a, index) => new RankingEntry
            {
                Rank = index + 1,
                StudentId = a.StudentId,
                StudentName = a.Student?.User?.FullName ?? string.Empty,
                Score = a.Score,
                SubmittedAt = a.EndTime
            }).ToList();

        return Ok(new ResponseResult<ExamRankingResponse>
        {
            Success = true,
            Message = "Success",
            Data = new ExamRankingResponse
            {
                ExamId = examId,
                ExamTitle = exam?.Title ?? string.Empty,
                Rankings = rankings
            }
        });
    }

    /// <summary>
    /// Get exam statistics
    /// </summary>
    [HttpGet("exam/{examId}/statistics")]
    public async Task<ActionResult<ResponseResult<ExamStatisticResponse>>> GetExamStatistics(long examId)
    {
        _logger.LogInformation("Getting statistics for exam: {ExamId}", examId);

        var (success, message, data) = await _statisticsService.GetExamStatisticsAsync(examId);
        if (!success)
            return NotFound(new ResponseResult<ExamStatisticResponse> { Success = false, Message = message });

        return Ok(new ResponseResult<ExamStatisticResponse> { Success = true, Message = message, Data = data });
    }
}
