using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

public class GradingService : IGradingService
{
    private readonly IGradingResultRepository _gradingRepo;
    private readonly IExamAttemptRepository _attemptRepo;
    private readonly IExamQuestionRepository _examQuestionRepo;
    private readonly IAnswerRepository _answerRepo;
    private readonly IQuestionOptionRepository _optionRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IExamRepository _examRepo;
    private readonly IExamSettingsRepository _examSettingsRepo;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLog;
    private readonly ILogger<GradingService> _logger;

    public GradingService(
        IGradingResultRepository gradingRepo,
        IExamAttemptRepository attemptRepo,
        IExamQuestionRepository examQuestionRepo,
        IAnswerRepository answerRepo,
        IQuestionOptionRepository optionRepo,
        IStudentRepository studentRepo,
        IExamRepository examRepo,
        IExamSettingsRepository examSettingsRepo,
        INotificationService notificationService,
        IActivityLogService activityLog,
        ILogger<GradingService> logger)
    {
        _gradingRepo = gradingRepo;
        _attemptRepo = attemptRepo;
        _examQuestionRepo = examQuestionRepo;
        _answerRepo = answerRepo;
        _optionRepo = optionRepo;
        _studentRepo = studentRepo;
        _examRepo = examRepo;
        _examSettingsRepo = examSettingsRepo;
        _notificationService = notificationService;
        _activityLog = activityLog;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, List<GradingResultResponse>? Data)> AutoGradeAttemptAsync(long attemptId)
    {
        try
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            if (attempt.Status != "SUBMITTED" && attempt.Status != "IN_PROGRESS")
                return (false, "Attempt cannot be auto-graded in its current state", null);

            var examQuestions = await _examQuestionRepo.GetExamQuestionsAsync(attempt.ExamId);
            var answers = await _answerRepo.GetByAttemptIdAsync(attemptId);
            var answerMap = answers.ToDictionary(a => a.QuestionId);

            var results = new List<GradingResultResponse>();
            decimal totalScore = 0m;

            foreach (var eq in examQuestions)
            {
                if (eq.Question == null) continue;

                var qTypeName = eq.Question.QuestionType?.Name?.ToUpperInvariant() ?? string.Empty;
                bool isAutoGradable = qTypeName is "MCQ" or "TRUE_FALSE";
                if (!isAutoGradable) continue;

                answerMap.TryGetValue(eq.QuestionId, out var answer);
                var correctOptions = await _optionRepo.GetCorrectOptionsAsync(new List<long> { eq.QuestionId });
                var correctOptionIds = correctOptions.Select(o => o.Id).ToHashSet();

                decimal questionScore = 0m;
                if (answer != null)
                {
                    var selectedOptionIds = answer.AnswerOptions?.Select(ao => ao.OptionId).ToHashSet() ?? new HashSet<long>();
                    if (selectedOptionIds.SetEquals(correctOptionIds))
                        questionScore = eq.MaxScore;
                }

                var existing = await _gradingRepo.GetByAttemptAndQuestionAsync(attemptId, eq.QuestionId);
                GradingResult graded;
                if (existing == null)
                {
                    graded = await _gradingRepo.CreateAsync(new GradingResult
                    {
                        ExamAttemptId = attemptId,
                        QuestionId = eq.QuestionId,
                        Score = questionScore,
                        GradedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Score = questionScore;
                    existing.GradedAt = DateTime.UtcNow;
                    graded = await _gradingRepo.UpdateAsync(existing);
                }

                totalScore += questionScore;
                results.Add(MapToGradingResponse(graded, eq, true));
            }

            var settings = await _examSettingsRepo.GetByExamIdAsync(attempt.ExamId);
            attempt.Score = ApplyLatePenalty(totalScore, attempt, settings);
            await _attemptRepo.UpdateAsync(attempt);

            return (true, "Auto-grading complete", results);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when auto-grading attempt {AttemptId}", attemptId);
            return (false, "Attempt was modified by another process. Please retry.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-grading attempt {AttemptId}", attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, AttemptGradingViewResponse? Data)> GetAttemptGradingViewAsync(long attemptId)
    {
        try
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            var exam = await _examRepo.GetByIdAsync(attempt.ExamId);
            var student = await _studentRepo.GetByIdAsync(attempt.StudentId);
            var examQuestions = await _examQuestionRepo.GetExamQuestionsAsync(attempt.ExamId);
            var answers = await _answerRepo.GetByAttemptIdAsync(attemptId);
            var gradingResults = await _gradingRepo.GetByAttemptIdAsync(attemptId);

            var answerMap = answers.ToDictionary(a => a.QuestionId);
            var gradingMap = gradingResults.ToDictionary(g => g.QuestionId);

            var items = new List<QuestionGradingItem>();
            foreach (var eq in examQuestions.OrderBy(q => q.QuestionOrder))
            {
                if (eq.Question == null) continue;

                answerMap.TryGetValue(eq.QuestionId, out var answer);
                gradingMap.TryGetValue(eq.QuestionId, out var grading);

                var allOptions = await _optionRepo.GetByQuestionIdAsync(eq.QuestionId);
                var selectedOptionIds = answer?.AnswerOptions?.Select(ao => ao.OptionId).ToHashSet() ?? new HashSet<long>();

                items.Add(new QuestionGradingItem
                {
                    QuestionId = eq.QuestionId,
                    Content = eq.Question.Content,
                    QuestionType = eq.Question.QuestionType?.Name ?? string.Empty,
                    Points = eq.MaxScore,
                    SelectedOptionIds = selectedOptionIds.ToList(),
                    TextContent = answer?.TextContent,
                    EssayContent = answer?.EssayContent,
                    CanvasImage = answer?.CanvasImage,
                    Options = allOptions.Select(o => new QuestionOptionGradeInfo
                    {
                        Id = o.Id,
                        Content = o.Content,
                        IsCorrect = o.IsCorrect,
                        WasSelected = selectedOptionIds.Contains(o.Id)
                    }).ToList(),
                    GradingResult = grading != null ? MapToGradingResponse(grading, eq, grading.GradedBy == null) : null
                });
            }

            return (true, "Success", new AttemptGradingViewResponse
            {
                AttemptId = attemptId,
                StudentId = attempt.StudentId,
                StudentName = student?.User?.FullName ?? string.Empty,
                ExamTitle = exam?.Title ?? string.Empty,
                Status = attempt.Status,
                TotalScore = attempt.Score,
                Questions = items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grading view for attempt {AttemptId}", attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<PendingGradingAttemptResponse>? Data)> GetPendingGradingAsync(long examId)
    {
        try
        {
            var exam = await _examRepo.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            var (attempts, _) = await _attemptRepo.GetAllAsync(1, 1000);
            var submitted = attempts.Where(a => a.ExamId == examId && a.Status == "SUBMITTED").ToList();

            var results = new List<PendingGradingAttemptResponse>();
            foreach (var attempt in submitted)
            {
                var student = await _studentRepo.GetByIdAsync(attempt.StudentId);
                var gradingResults = await _gradingRepo.GetByAttemptIdAsync(attempt.Id);
                var examQuestions = await _examQuestionRepo.GetExamQuestionsAsync(examId);
                var manualQuestionCount = examQuestions.Count(eq =>
                {
                    var typeName = eq.Question?.QuestionType?.Name?.ToUpperInvariant() ?? string.Empty;
                    return typeName is not "MCQ" and not "TRUE_FALSE";
                });
                var gradedManualCount = gradingResults.Count(g => g.GradedBy != null);
                bool hasUngraded = gradedManualCount < manualQuestionCount;

                results.Add(new PendingGradingAttemptResponse
                {
                    AttemptId = attempt.Id,
                    StudentId = attempt.StudentId,
                    StudentName = student?.User?.FullName ?? string.Empty,
                    SubmittedAt = attempt.EndTime ?? attempt.StartTime,
                    HasUngraded = hasUngraded
                });
            }

            return (true, "Success", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending grading for exam {ExamId}", examId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, GradingResultResponse? Data)> ManualGradeQuestionAsync(long attemptId, long questionId, ManualGradeRequest request, long gradedBy)
    {
        try
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            var examQuestion = await _examQuestionRepo.GetExamQuestionAsync(attempt.ExamId, questionId);
            if (examQuestion == null)
                return (false, "Question not found in this exam", null);

            if (request.Score < 0 || request.Score > examQuestion.MaxScore)
                return (false, $"Score must be between 0 and {examQuestion.MaxScore}", null);

            var existing = await _gradingRepo.GetByAttemptAndQuestionAsync(attemptId, questionId);
            GradingResult result;
            if (existing == null)
            {
                result = await _gradingRepo.CreateAsync(new GradingResult
                {
                    ExamAttemptId = attemptId,
                    QuestionId = questionId,
                    Score = request.Score,
                    Comment = request.Comment,
                    Annotations = request.Annotations,
                    GradedBy = gradedBy,
                    GradedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Score = request.Score;
                existing.Comment = request.Comment;
                existing.Annotations = request.Annotations;
                existing.GradedBy = gradedBy;
                existing.GradedAt = DateTime.UtcNow;
                result = await _gradingRepo.UpdateAsync(existing);
            }

            await _activityLog.LogAsync(
                gradedBy,
                "GRADE_UPDATED",
                "GradingResult",
                result.Id,
                $"AttemptId: {attemptId}, QuestionId: {questionId}, Score: {result.Score}");

            return (true, "Question graded", MapToGradingResponse(result, examQuestion, false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error manually grading question {QuestionId} for attempt {AttemptId}", questionId, attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<GradingResultResponse>? Data)> BatchGradeAsync(long attemptId, BatchGradeRequest request, long gradedBy)
    {
        try
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            var results = new List<GradingResultResponse>();
            foreach (var item in request.Grades)
            {
                var examQuestion = await _examQuestionRepo.GetExamQuestionAsync(attempt.ExamId, item.QuestionId);
                if (examQuestion == null) continue;

                if (item.Score < 0 || item.Score > examQuestion.MaxScore)
                    return (false, $"Score for question {item.QuestionId} must be between 0 and {examQuestion.MaxScore}", null);

                var existing = await _gradingRepo.GetByAttemptAndQuestionAsync(attemptId, item.QuestionId);
                GradingResult result;
                if (existing == null)
                {
                    result = await _gradingRepo.CreateAsync(new GradingResult
                    {
                        ExamAttemptId = attemptId,
                        QuestionId = item.QuestionId,
                        Score = item.Score,
                        Comment = item.Comment,
                        Annotations = item.Annotations,
                        GradedBy = gradedBy,
                        GradedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Score = item.Score;
                    existing.Comment = item.Comment;
                    existing.Annotations = item.Annotations;
                    existing.GradedBy = gradedBy;
                    existing.GradedAt = DateTime.UtcNow;
                    result = await _gradingRepo.UpdateAsync(existing);
                }

                await _activityLog.LogAsync(
                    gradedBy,
                    "GRADE_UPDATED",
                    "GradingResult",
                    result.Id,
                    $"AttemptId: {attemptId}, QuestionId: {item.QuestionId}, Score: {result.Score}");

                results.Add(MapToGradingResponse(result, examQuestion, false));
            }

            return (true, "Batch grading complete", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch grading attempt {AttemptId}", attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> MarkAsGradedAsync(long attemptId)
    {
        try
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found");

            if (attempt.Status != "SUBMITTED")
                return (false, "Attempt must be in SUBMITTED status");

            var gradingResults = await _gradingRepo.GetByAttemptIdAsync(attemptId);
            var totalScore = gradingResults.Sum(g => g.Score);
            var settings = await _examSettingsRepo.GetByExamIdAsync(attempt.ExamId);

            attempt.Status = "GRADED";
            attempt.Score = ApplyLatePenalty(totalScore, attempt, settings);
            await _attemptRepo.UpdateAsync(attempt);

            return (true, "Attempt marked as graded");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when marking graded attempt {AttemptId}", attemptId);
            return (false, "Attempt was modified by another process. Please reload and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking attempt {AttemptId} as graded", attemptId);
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, PublishResultResponse? Data)> PublishResultAsync(long attemptId)
    {
        try
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            if (attempt.Status != "GRADED")
                return (false, "Attempt must be in GRADED status to publish results", null);

            attempt.IsResultPublished = true;
            await _attemptRepo.UpdateAsync(attempt);
            await _activityLog.LogAsync(null, "GRADE_PUBLISHED", "ExamAttempt", attemptId);

            var student = await _studentRepo.GetByIdAsync(attempt.StudentId);
            var exam = await _examRepo.GetByIdAsync(attempt.ExamId);
            if (student != null)
            {
                var title = "Ket qua bai thi da duoc cong bo";
                var message = $"Bai thi '{exam?.Title ?? "(Unknown Exam)"}' da co ket qua. Diem hien tai: {attempt.Score?.ToString("0.##") ?? "N/A"}.";
                await _notificationService.CreateAsync(
                    student.UserId,
                    "GRADE_PUBLISHED",
                    title,
                    message,
                    attemptId,
                    "ExamAttempt");
            }

            return (true, "Result published", new PublishResultResponse
            {
                AttemptId = attemptId,
                TotalScore = attempt.Score,
                Status = attempt.Status,
                Published = true
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when publishing result for attempt {AttemptId}", attemptId);
            return (false, "Attempt was modified by another process. Please reload and retry.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing result for attempt {AttemptId}", attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, AttemptGradingViewResponse? Data)> GetStudentResultAsync(long attemptId)
    {
        try
        {
            var attempt = await _attemptRepo.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            var result = await GetAttemptGradingViewAsync(attemptId);
            if (!result.Success) return result;

            // If results are not published, hide grading details
            if (!attempt.IsResultPublished && result.Data != null)
            {
                foreach (var q in result.Data.Questions)
                {
                    q.GradingResult = null;
                    // Hide correct answers for MCQ
                    foreach (var opt in q.Options)
                        opt.IsCorrect = false;
                }
                result.Data.TotalScore = null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student result for attempt {AttemptId}", attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    private static GradingResultResponse MapToGradingResponse(GradingResult g, ExamQuestion eq, bool isAutoGraded) => new()
    {
        Id = g.Id,
        ExamAttemptId = g.ExamAttemptId,
        QuestionId = g.QuestionId,
        QuestionContent = eq.Question?.Content ?? string.Empty,
        QuestionType = eq.Question?.QuestionType?.Name ?? string.Empty,
        Points = eq.MaxScore,
        Score = g.Score,
        Comment = g.Comment,
        Annotations = g.Annotations,
        GradedBy = g.GradedBy,
        GradedAt = g.GradedAt,
        IsAutoGraded = isAutoGraded
    };

    private static decimal ApplyLatePenalty(decimal rawScore, ExamAttempt attempt, ExamSetting? settings)
    {
        if (!attempt.IsLateSubmission)
            return rawScore;

        var penaltyPercent = attempt.LatePenaltyPercent > 0m
            ? attempt.LatePenaltyPercent
            : settings?.LatePenaltyPercent ?? 0m;

        penaltyPercent = Math.Clamp(penaltyPercent, 0m, 100m);
        if (penaltyPercent <= 0m)
            return rawScore;

        return decimal.Round(rawScore * (1m - (penaltyPercent / 100m)), 2, MidpointRounding.AwayFromZero);
    }
}
