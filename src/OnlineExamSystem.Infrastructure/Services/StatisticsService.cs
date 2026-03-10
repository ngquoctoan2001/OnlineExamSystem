using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IExamStatisticRepository _statRepo;
    private readonly IExamAttemptRepository _attemptRepo;
    private readonly IExamRepository _examRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IClassRepository _classRepo;
    private readonly ILogger<StatisticsService> _logger;

    private const decimal PassThreshold = 5m; // score out of 10 considered passing

    public StatisticsService(
        IExamStatisticRepository statRepo,
        IExamAttemptRepository attemptRepo,
        IExamRepository examRepo,
        IStudentRepository studentRepo,
        IClassRepository classRepo,
        ILogger<StatisticsService> logger)
    {
        _statRepo = statRepo;
        _attemptRepo = attemptRepo;
        _examRepo = examRepo;
        _studentRepo = studentRepo;
        _classRepo = classRepo;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ExamStatisticResponse? Data)> CalculateAndSaveExamStatisticsAsync(long examId)
    {
        try
        {
            var exam = await _examRepo.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            var attempts = await _attemptRepo.GetExamAttemptsAsync(examId);
            var graded = attempts.Where(a => a.Status is "GRADED" or "SUBMITTED" && a.Score.HasValue).ToList();

            if (!graded.Any())
            {
                var empty = new ExamStatistic
                {
                    ExamId = examId,
                    TotalAttempts = attempts.Count,
                    PassCount = 0,
                    FailCount = 0,
                    AverageScore = 0,
                    MaxScore = 0,
                    MinScore = 0,
                    CalculatedAt = DateTime.UtcNow
                };
                var saved = await _statRepo.UpdateOrCreateAsync(empty);
                return (true, "Statistics calculated (no graded attempts)", MapToResponse(saved, exam.Title));
            }

            var totalScore = exam.TotalScore > 0 ? exam.TotalScore : 10;
            var scores = graded.Select(a => a.Score!.Value).ToList();
            var passScore = totalScore * (PassThreshold / 10m);
            var passCount = scores.Count(s => s >= passScore);

            var stat = new ExamStatistic
            {
                ExamId = examId,
                TotalAttempts = attempts.Count,
                PassCount = passCount,
                FailCount = graded.Count - passCount,
                AverageScore = Math.Round(scores.Average(), 2),
                MaxScore = scores.Max(),
                MinScore = scores.Min(),
                CalculatedAt = DateTime.UtcNow
            };

            var result = await _statRepo.UpdateOrCreateAsync(stat);
            return (true, "Statistics calculated", MapToResponse(result, exam.Title));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics for exam {ExamId}", examId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamStatisticResponse? Data)> GetExamStatisticsAsync(long examId)
    {
        try
        {
            var exam = await _examRepo.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            var stat = await _statRepo.GetByExamIdAsync(examId);
            if (stat == null)
                return (false, "Statistics not yet calculated. Use POST /calculate first.", null);

            return (true, "Success", MapToResponse(stat, exam.Title));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for exam {ExamId}", examId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ScoreDistributionResponse? Data)> GetScoreDistributionAsync(long examId)
    {
        try
        {
            var exam = await _examRepo.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            var attempts = (await _attemptRepo.GetExamAttemptsAsync(examId))
                .Where(a => a.Score.HasValue).ToList();

            var totalScore = exam.TotalScore > 0 ? (decimal)exam.TotalScore : 10m;
            var buckets = new List<ScoreBucket>
            {
                new() { Label = "0-2", Min = 0, Max = totalScore * 0.2m, Count = 0 },
                new() { Label = "2-4", Min = totalScore * 0.2m, Max = totalScore * 0.4m, Count = 0 },
                new() { Label = "4-6", Min = totalScore * 0.4m, Max = totalScore * 0.6m, Count = 0 },
                new() { Label = "6-8", Min = totalScore * 0.6m, Max = totalScore * 0.8m, Count = 0 },
                new() { Label = "8-10", Min = totalScore * 0.8m, Max = totalScore, Count = 0 }
            };

            foreach (var attempt in attempts)
            {
                var score = attempt.Score!.Value;
                var bucket = buckets.LastOrDefault(b => score >= b.Min);
                if (bucket != null) bucket.Count++;
            }

            return (true, "Success", new ScoreDistributionResponse { ExamId = examId, Buckets = buckets });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting score distribution for exam {ExamId}", examId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, StudentPerformanceResponse? Data)> GetStudentPerformanceAsync(long studentId)
    {
        try
        {
            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student == null)
                return (false, "Student not found", null);

            var attempts = await _attemptRepo.GetStudentAttemptsAsync(studentId);
            var summaries = new List<StudentAttemptSummary>();

            foreach (var attempt in attempts)
            {
                var exam = await _examRepo.GetByIdAsync(attempt.ExamId);
                summaries.Add(new StudentAttemptSummary
                {
                    AttemptId = attempt.Id,
                    StudentName = student?.User?.FullName ?? string.Empty,
                    ExamId = attempt.ExamId,
                    ExamTitle = exam?.Title ?? string.Empty,
                    SubjectName = exam?.Subject?.Name ?? string.Empty,
                    Score = attempt.Score,
                    Status = attempt.Status,
                    StartTime = attempt.StartTime,
                    EndTime = attempt.EndTime
                });
            }

            var scoredAttempts = summaries.Where(s => s.Score.HasValue).ToList();
            return (true, "Success", new StudentPerformanceResponse
            {
                StudentId = studentId,
                StudentName = student.User?.FullName ?? string.Empty,
                TotalAttempts = attempts.Count,
                AverageScore = scoredAttempts.Any() ? Math.Round(scoredAttempts.Average(s => s.Score!.Value), 2) : 0,
                Attempts = summaries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance for student {StudentId}", studentId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ClassResultsResponse? Data)> GetClassResultsAsync(long classId, long examId)
    {
        try
        {
            var classEntity = await _classRepo.GetByIdAsync(classId);
            if (classEntity == null)
                return (false, "Class not found", null);

            var exam = await _examRepo.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            var classStudents = await _classRepo.GetClassStudentsAsync(classId);
            var studentIds = classStudents.Select(cs => cs.StudentId).ToList();

            var allAttempts = await _attemptRepo.GetExamAttemptsAsync(examId);
            var classAttempts = allAttempts.Where(a => studentIds.Contains(a.StudentId)).ToList();

            var summaries = new List<StudentAttemptSummary>();
            foreach (var attempt in classAttempts)
            {
                var student = await _studentRepo.GetByIdAsync(attempt.StudentId);
                summaries.Add(new StudentAttemptSummary
                {
                    AttemptId = attempt.Id,
                    StudentName = student?.User?.FullName ?? string.Empty,
                    ExamId = examId,
                    ExamTitle = exam.Title,
                    SubjectName = exam.Subject?.Name ?? string.Empty,
                    Score = attempt.Score,
                    Status = attempt.Status,
                    StartTime = attempt.StartTime,
                    EndTime = attempt.EndTime
                });
            }

            var scored = summaries.Where(s => s.Score.HasValue).ToList();
            return (true, "Success", new ClassResultsResponse
            {
                ClassId = classId,
                ClassName = classEntity.Name,
                ExamId = examId,
                ExamTitle = exam.Title,
                TotalStudents = studentIds.Count,
                AttemptedCount = classAttempts.Count,
                AverageScore = scored.Any() ? Math.Round(scored.Average(s => s.Score!.Value), 2) : 0,
                StudentResults = summaries
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting class results for class {ClassId}, exam {ExamId}", classId, examId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    private static ExamStatisticResponse MapToResponse(ExamStatistic stat, string examTitle) => new()
    {
        ExamId = stat.ExamId,
        ExamTitle = examTitle,
        TotalAttempts = stat.TotalAttempts,
        PassCount = stat.PassCount,
        FailCount = stat.FailCount,
        PassRate = stat.TotalAttempts > 0 ? Math.Round((decimal)stat.PassCount / stat.TotalAttempts * 100, 1) : 0,
        AverageScore = stat.AverageScore,
        MaxScore = stat.MaxScore,
        MinScore = stat.MinScore,
        CalculatedAt = stat.CalculatedAt
    };
}
