using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

public class ExamAttemptService : IExamAttemptService
{
    private readonly IExamAttemptRepository _examAttemptRepository;
    private readonly IExamRepository _examRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<ExamAttemptService> _logger;

    public ExamAttemptService(
        IExamAttemptRepository examAttemptRepository,
        IExamRepository examRepository,
        IStudentRepository studentRepository,
        ILogger<ExamAttemptService> logger)
    {
        _examAttemptRepository = examAttemptRepository;
        _examRepository = examRepository;
        _studentRepository = studentRepository;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ExamAttemptResponse? Data)> StartAttemptAsync(long examId, long studentId)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            if (exam.Status != "ACTIVE")
                return (false, "Exam is not active", null);

            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null)
                return (false, "Student not found", null);

            var existingAttempt = await _examAttemptRepository.GetStudentExamAttemptAsync(studentId, examId);
            if (existingAttempt != null)
                return (false, "Student already has an active attempt for this exam", null);

            var attempt = new ExamAttempt
            {
                ExamId = examId,
                StudentId = studentId,
                StartTime = DateTime.UtcNow,
                Status = "IN_PROGRESS"
            };

            await _examAttemptRepository.CreateAsync(attempt);

            return (true, "Exam attempt started successfully", new ExamAttemptResponse
            {
                Id = attempt.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                StudentId = student.Id,
                StudentName = student.User?.FullName ?? string.Empty,
                Status = attempt.Status,
                StartTime = attempt.StartTime,
                EndTime = attempt.EndTime,
                Score = attempt.Score,
                TotalQuestions = 0,
                AnsweredQuestions = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting exam attempt");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamAttemptResponse? Data)> GetAttemptByIdAsync(long attemptId)
    {
        try
        {
            var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            var student = await _studentRepository.GetByIdAsync(attempt.StudentId);

            return (true, "Success", MapToResponse(attempt, exam, student));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam attempt");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamAttemptResponse? Data)> GetCurrentAttemptAsync(long studentId, long examId)
    {
        try
        {
            var attempt = await _examAttemptRepository.GetStudentExamAttemptAsync(studentId, examId);
            if (attempt == null)
                return (false, "No active attempt found", null);

            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            var student = await _studentRepository.GetByIdAsync(attempt.StudentId);

            return (true, "Success", MapToResponse(attempt, exam, student));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current exam attempt");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ExamAttemptResponse>? Data)> GetStudentAttemptsAsync(long studentId)
    {
        try
        {
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null)
                return (false, "Student not found", null);

            var attempts = await _examAttemptRepository.GetStudentAttemptsAsync(studentId);
            var responses = new List<ExamAttemptResponse>();

            foreach (var attempt in attempts)
            {
                var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
                responses.Add(MapToResponse(attempt, exam, student));
            }

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student exam attempts");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamAttemptListResponse? Data)> GetExamAttemptsAsync(long examId, int page = 1, int pageSize = 20)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            var (attempts, totalCount) = await _examAttemptRepository.GetAllAsync(page, pageSize);
            attempts = attempts.Where(a => a.ExamId == examId).ToList();

            var responses = new List<ExamAttemptResponse>();
            foreach (var attempt in attempts)
            {
                var student = await _studentRepository.GetByIdAsync(attempt.StudentId);
                responses.Add(MapToResponse(attempt, exam, student));
            }

            return (true, "Success", new ExamAttemptListResponse
            {
                Items = responses,
                Page = page,
                PageSize = pageSize,
                TotalCount = attempts.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam attempts");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, SubmitExamAttemptResponse? Data)> SubmitAttemptAsync(long attemptId)
    {
        try
        {
            var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            if (attempt.Status != "IN_PROGRESS")
                return (false, "Attempt is not in progress", null);

            attempt.Status = "SUBMITTED";
            attempt.EndTime = DateTime.UtcNow;

            await _examAttemptRepository.UpdateAsync(attempt);

            return (true, "Exam attempt submitted successfully", new SubmitExamAttemptResponse
            {
                AttemptId = attempt.Id,
                Status = attempt.Status,
                SubmittedAt = attempt.EndTime ?? DateTime.UtcNow,
                Message = "Your exam has been submitted"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting exam attempt");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamAttemptDetailResponse? Data)> GetAttemptDetailAsync(long attemptId)
    {
        try
        {
            var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            var student = await _studentRepository.GetByIdAsync(attempt.StudentId);

            return (true, "Success", new ExamAttemptDetailResponse
            {
                Id = attempt.Id,
                ExamId = exam?.Id ?? 0,
                ExamTitle = exam?.Title,
                StudentId = student?.Id ?? 0,
                StudentName = student?.User?.FullName ?? string.Empty,
                Status = attempt.Status,
                StartTime = attempt.StartTime,
                EndTime = attempt.EndTime,
                Score = attempt.Score,
                Answers = attempt.Answers?.Select(a => new AttemptAnswerResponse
                {
                    AnswerId = a.Id,
                    QuestionId = a.QuestionId,
                    TextContent = a.TextContent,
                    EssayContent = a.EssayContent,
                    CanvasImage = a.CanvasImage
                }).ToList() ?? new()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam attempt detail");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamAttemptListResponse? Data)> GetAllAttemptsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (attempts, totalCount) = await _examAttemptRepository.GetAllAsync(page, pageSize);

            var responses = new List<ExamAttemptResponse>();
            foreach (var attempt in attempts)
            {
                var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
                var student = await _studentRepository.GetByIdAsync(attempt.StudentId);
                responses.Add(MapToResponse(attempt, exam, student));
            }

            return (true, "Success", new ExamAttemptListResponse
            {
                Items = responses,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all exam attempts");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    private ExamAttemptResponse MapToResponse(ExamAttempt attempt, Exam? exam, Student? student)
    {
        return new ExamAttemptResponse
        {
            Id = attempt.Id,
            ExamId = exam?.Id ?? 0,
            ExamTitle = exam?.Title,
            StudentId = student?.Id ?? 0,
            StudentName = student?.User?.FullName ?? string.Empty,
            Status = attempt.Status,
            StartTime = attempt.StartTime,
            EndTime = attempt.EndTime,
            Score = attempt.Score,
            TotalQuestions = attempt.Answers?.Count ?? 0,
            AnsweredQuestions = attempt.Answers?.Where(a => !string.IsNullOrEmpty(a.TextContent) || !string.IsNullOrEmpty(a.EssayContent) || !string.IsNullOrEmpty(a.CanvasImage)).Count() ?? 0
        };
    }
}
