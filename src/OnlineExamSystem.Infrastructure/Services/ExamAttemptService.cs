using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

public class ExamAttemptService : IExamAttemptService
{
    private readonly IExamAttemptRepository _examAttemptRepository;
    private readonly IExamRepository _examRepository;
    private readonly IExamSettingsRepository _examSettingsRepository;
    private readonly IExamClassRepository _examClassRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IExamViolationRepository _violationRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly IGradingResultRepository _gradingResultRepository;
    private readonly IExamQuestionRepository _examQuestionRepository;
    private readonly IQuestionOptionRepository _optionRepository;
    private readonly IActivityLogService _activityLog;
    private readonly ILogger<ExamAttemptService> _logger;

    public ExamAttemptService(
        IExamAttemptRepository examAttemptRepository,
        IExamRepository examRepository,
        IExamSettingsRepository examSettingsRepository,
        IExamClassRepository examClassRepository,
        IStudentRepository studentRepository,
        IExamViolationRepository violationRepository,
        IAnswerRepository answerRepository,
        IGradingResultRepository gradingResultRepository,
        IExamQuestionRepository examQuestionRepository,
        IQuestionOptionRepository optionRepository,
        IActivityLogService activityLog,
        ILogger<ExamAttemptService> logger)
    {
        _examAttemptRepository = examAttemptRepository;
        _examRepository = examRepository;
        _examSettingsRepository = examSettingsRepository;
        _examClassRepository = examClassRepository;
        _studentRepository = studentRepository;
        _violationRepository = violationRepository;
        _answerRepository = answerRepository;
        _gradingResultRepository = gradingResultRepository;
        _examQuestionRepository = examQuestionRepository;
        _optionRepository = optionRepository;
        _activityLog = activityLog;
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

            // Enforce exam availability window.
            var now = DateTime.UtcNow;
            if (exam.StartTime != default && now < exam.StartTime)
                return (false, $"Exam starts at {exam.StartTime:yyyy-MM-dd HH:mm} UTC", null);

            if (exam.EndTime != default && now > exam.EndTime)
                return (false, "Exam window closed", null);

            // Enforce enrollment: student must belong to at least one class assigned to this exam.
            var studentClassIds = (await _studentRepository.GetStudentClassesAsync(studentId))
                .Select(cs => cs.ClassId)
                .Distinct()
                .ToHashSet();

            var examClassIds = (await _examClassRepository.GetExamClassesAsync(examId))
                .Select(ec => ec.ClassId)
                .Distinct()
                .ToHashSet();

            var isEnrolled = studentClassIds.Overlaps(examClassIds);
            if (!isEnrolled)
            {
                _logger.LogWarning(
                    "Unauthorized exam attempt start blocked: Student {StudentId} is not assigned to exam {ExamId}",
                    studentId, examId);
                return (false, "Student not enrolled for this exam", null);
            }

            var existingAttempt = await _examAttemptRepository.GetStudentExamAttemptAsync(studentId, examId);
            if (existingAttempt != null)
                return (false, "Student already has an active attempt for this exam", null);

            var examAttempts = await _examAttemptRepository.GetExamAttemptsAsync(examId);
            var studentAttempts = examAttempts
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.EndTime ?? a.StartTime)
                .ToList();

            if (exam.MaxAttemptsAllowed > 0 && studentAttempts.Count >= exam.MaxAttemptsAllowed)
                return (false, $"Maximum attempts reached ({exam.MaxAttemptsAllowed})", null);

            var completedAttempts = studentAttempts
                .Where(a => !string.Equals(a.Status, "IN_PROGRESS", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!exam.AllowRetakeIfPassed)
            {
                var passingScore = Math.Clamp(exam.PassingScore, 0m, 100m);
                var hasPassed = completedAttempts.Any(a => a.Score.HasValue && a.Score.Value >= passingScore);
                if (hasPassed)
                    return (false, "Student already passed this exam and retake is not allowed", null);
            }

            if (exam.MinutesBetweenRetakes > 0 && completedAttempts.Count > 0)
            {
                var lastAttempt = completedAttempts[0];
                var lastAttemptTime = lastAttempt.EndTime ?? lastAttempt.StartTime;
                var nextAllowedTime = lastAttemptTime.AddMinutes(exam.MinutesBetweenRetakes);
                if (now < nextAllowedTime)
                {
                    var waitMinutes = Math.Ceiling((nextAllowedTime - now).TotalMinutes);
                    return (false, $"Must wait {waitMinutes} minute(s) before retaking", null);
                }
            }

            var attempt = new ExamAttempt
            {
                ExamId = examId,
                StudentId = studentId,
                StartTime = DateTime.UtcNow,
                Status = "IN_PROGRESS"
            };

            await _examAttemptRepository.CreateAsync(attempt);
            await _activityLog.LogAsync(studentId, "EXAM_STARTED", "ExamAttempt", attempt.Id, $"ExamId: {examId}");

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

                // Auto-expire stale IN_PROGRESS attempts whose time has run out
                if (attempt.Status == "IN_PROGRESS" && exam != null)
                {
                    var now = DateTime.UtcNow;
                    var settings = await _examSettingsRepository.GetByExamIdAsync(exam.Id);
                    var hardDeadline = GetHardDeadline(exam, attempt.StartTime);
                    var finalDeadline = GetFinalSubmissionDeadline(hardDeadline, settings);
                    if (now > finalDeadline)
                    {
                        attempt.Status = "SUBMITTED";
                        attempt.EndTime = finalDeadline;
                        attempt.IsLateSubmission = now > hardDeadline;
                        attempt.LatePenaltyPercent = attempt.IsLateSubmission
                            ? NormalizePenaltyPercent(settings?.LatePenaltyPercent ?? 0m)
                            : 0m;
                        await _examAttemptRepository.UpdateAsync(attempt);
                        // Auto-grade what we can
                        var autoScore = await AutoGradeAttemptAsync(attempt);
                        if (autoScore.HasValue)
                        {
                            attempt.Score = autoScore;
                            await _examAttemptRepository.UpdateAsync(attempt);
                        }
                        _logger.LogInformation("Auto-expired stale attempt {AttemptId} for exam {ExamId}", attempt.Id, attempt.ExamId);
                    }
                }

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

            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            if (exam == null)
                return (false, "Exam not found", null);

            var settings = await _examSettingsRepository.GetByExamIdAsync(attempt.ExamId);

            var now = DateTime.UtcNow;
            var (isAllowed, isLateSubmission, rejectionReason) = EvaluateSubmissionTiming(exam, attempt.StartTime, settings, now);
            if (!isAllowed)
                return (false, rejectionReason!, null);

            attempt.Status = "SUBMITTED";
            attempt.EndTime = now;
            attempt.IsLateSubmission = isLateSubmission;
            attempt.LatePenaltyPercent = isLateSubmission
                ? NormalizePenaltyPercent(settings?.LatePenaltyPercent ?? 0m)
                : 0m;

            await _examAttemptRepository.UpdateAsync(attempt);

            // Auto-grade MCQ and TRUE_FALSE questions
            var autoScore = await AutoGradeAttemptAsync(attempt);
            if (autoScore.HasValue)
            {
                attempt.Score = autoScore;
                await _examAttemptRepository.UpdateAsync(attempt);
            }

            await _activityLog.LogAsync(null, "EXAM_SUBMITTED", "ExamAttempt", attempt.Id, $"ExamId: {attempt.ExamId}, StudentId: {attempt.StudentId}");
            var submitMessage = attempt.IsLateSubmission
                ? $"Your exam has been submitted late. Penalty applied: {attempt.LatePenaltyPercent:0.##}%"
                : "Your exam has been submitted";
            return (true, "Exam attempt submitted successfully", new SubmitExamAttemptResponse
            {
                AttemptId = attempt.Id,
                Status = attempt.Status,
                SubmittedAt = attempt.EndTime ?? now,
                IsLateSubmission = attempt.IsLateSubmission,
                LatePenaltyPercent = attempt.LatePenaltyPercent,
                Message = submitMessage
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when submitting attempt {AttemptId}", attemptId);
            return (false, "Attempt was modified by another process. Please reload and try again.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting exam attempt");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    private async Task<decimal?> AutoGradeAttemptAsync(ExamAttempt attempt)
    {
        try
        {
            var settings = await _examSettingsRepository.GetByExamIdAsync(attempt.ExamId);
            var examQuestions = await _examQuestionRepository.GetExamQuestionsAsync(attempt.ExamId);
            var answers = await _answerRepository.GetByAttemptIdAsync(attempt.Id);
            var answerMap = answers.ToDictionary(a => a.QuestionId);

            decimal totalScore = 0m;
            bool anyAutoGraded = false;

            foreach (var eq in examQuestions)
            {
                if (eq.Question == null) continue;

                var qTypeName = eq.Question.QuestionType?.Name?.ToUpperInvariant() ?? string.Empty;
                bool isAutoGradable = qTypeName is "MCQ" or "TRUE_FALSE";

                if (!isAutoGradable) continue;

                answerMap.TryGetValue(eq.QuestionId, out var answer);
                var correctOptions = await _optionRepository.GetCorrectOptionsAsync(new List<long> { eq.QuestionId });
                var correctOptionIds = correctOptions.Select(o => o.Id).ToHashSet();

                decimal questionScore = 0m;
                if (answer != null)
                {
                    var selectedOptionIds = answer.AnswerOptions?.Select(ao => ao.OptionId).ToHashSet() ?? new HashSet<long>();
                    // Full marks only if selected options exactly match correct options
                    if (selectedOptionIds.SetEquals(correctOptionIds))
                        questionScore = eq.MaxScore;
                }

                var existing = await _gradingResultRepository.GetByAttemptAndQuestionAsync(attempt.Id, eq.QuestionId);
                if (existing == null)
                {
                    await _gradingResultRepository.CreateAsync(new Domain.Entities.GradingResult
                    {
                        ExamAttemptId = attempt.Id,
                        QuestionId = eq.QuestionId,
                        Score = questionScore,
                        GradedAt = DateTime.UtcNow,
                        GradedBy = null // auto-graded
                    });
                }
                else
                {
                    existing.Score = questionScore;
                    existing.GradedAt = DateTime.UtcNow;
                    await _gradingResultRepository.UpdateAsync(existing);
                }

                totalScore += questionScore;
                anyAutoGraded = true;
            }

            if (!anyAutoGraded)
                return null;

            return ApplyLatePenalty(totalScore, attempt, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-grading for attempt {AttemptId}", attempt.Id);
            return null;
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
        // Calculate total points from exam questions
        decimal? totalPoints = exam?.ExamQuestions?.Sum(eq => eq.MaxScore);

        return new ExamAttemptResponse
        {
            Id = attempt.Id,
            ExamId = exam?.Id ?? 0,
            ExamTitle = exam?.Title,
            StudentId = student?.Id ?? 0,
            StudentName = student?.User?.FullName ?? string.Empty,
            StudentCode = student?.StudentCode ?? string.Empty,
            Status = attempt.Status,
            StartTime = attempt.StartTime,
            EndTime = attempt.EndTime,
            Score = attempt.Score,
            TotalPoints = totalPoints,
            IsPassed = attempt.Score.HasValue && totalPoints.HasValue && totalPoints.Value > 0
                ? attempt.Score.Value >= (totalPoints.Value * 0.5m)
                : null,
            TotalQuestions = attempt.Answers?.Count ?? 0,
            AnsweredQuestions = attempt.Answers?.Where(a => !string.IsNullOrEmpty(a.TextContent) || !string.IsNullOrEmpty(a.EssayContent) || !string.IsNullOrEmpty(a.CanvasImage)).Count() ?? 0
        };
    }

    public async Task<(bool Success, string Message, ViolationResponse? Data)> LogViolationAsync(long attemptId, LogViolationRequest request)
    {
        try
        {
            var attempt = await _examAttemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            if (attempt.Status != "IN_PROGRESS")
                return (false, "Exam attempt is not in progress", null);

            var violation = new ExamViolation
            {
                ExamAttemptId = attemptId,
                ViolationType = request.ViolationType,
                Description = request.Description,
                OccurredAt = DateTime.UtcNow
            };

            await _violationRepository.CreateAsync(violation);

            return (true, "Violation logged", new ViolationResponse
            {
                Id = violation.Id,
                ExamAttemptId = violation.ExamAttemptId,
                ViolationType = violation.ViolationType,
                Description = violation.Description,
                OccurredAt = violation.OccurredAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging violation for attempt {AttemptId}", attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    private static decimal ApplyLatePenalty(decimal rawScore, ExamAttempt attempt, ExamSetting? settings)
    {
        if (!attempt.IsLateSubmission)
            return rawScore;

        var penaltyPercent = NormalizePenaltyPercent(attempt.LatePenaltyPercent > 0m
            ? attempt.LatePenaltyPercent
            : settings?.LatePenaltyPercent ?? 0m);

        if (penaltyPercent <= 0m)
            return rawScore;

        return decimal.Round(rawScore * (1m - (penaltyPercent / 100m)), 2, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizePenaltyPercent(decimal penaltyPercent)
    {
        return Math.Clamp(penaltyPercent, 0m, 100m);
    }

    private static DateTime GetHardDeadline(Exam exam, DateTime attemptStartTime)
    {
        var byDuration = exam.DurationMinutes > 0
            ? attemptStartTime.AddMinutes(exam.DurationMinutes)
            : DateTime.MaxValue;

        var byExamWindow = exam.EndTime != default
            ? exam.EndTime
            : DateTime.MaxValue;

        return byDuration < byExamWindow ? byDuration : byExamWindow;
    }

    private static DateTime GetFinalSubmissionDeadline(DateTime hardDeadline, ExamSetting? settings)
    {
        if (settings?.AllowLateSubmission != true)
            return hardDeadline;

        var grace = Math.Max(0, settings.GracePeriodMinutes);
        return hardDeadline.AddMinutes(grace);
    }

    private static (bool IsAllowed, bool IsLateSubmission, string? RejectionReason) EvaluateSubmissionTiming(
        Exam exam,
        DateTime attemptStartTime,
        ExamSetting? settings,
        DateTime now)
    {
        if (exam.StartTime != default && now < exam.StartTime)
            return (false, false, "Exam has not started yet");

        var hardDeadline = GetHardDeadline(exam, attemptStartTime);
        if (now <= hardDeadline)
            return (true, false, null);

        if (settings?.AllowLateSubmission != true)
            return (false, false, "Exam duration exceeded");

        var finalDeadline = GetFinalSubmissionDeadline(hardDeadline, settings);
        if (now > finalDeadline)
            return (false, false, "Late submission window has closed");

        return (true, true, null);
    }
}
