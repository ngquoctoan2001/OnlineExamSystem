using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

public class AnswerService : IAnswerService
{
    private readonly IAnswerRepository _answerRepository;
    private readonly IExamAttemptRepository _attemptRepository;
    private readonly IExamRepository _examRepository;
    private readonly IExamSettingsRepository _examSettingsRepository;
    private readonly IExamQuestionRepository _examQuestionRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionOptionRepository _optionRepository;
    private readonly ILogger<AnswerService> _logger;

    public AnswerService(
        IAnswerRepository answerRepository,
        IExamAttemptRepository attemptRepository,
        IExamRepository examRepository,
        IExamSettingsRepository examSettingsRepository,
        IExamQuestionRepository examQuestionRepository,
        IQuestionRepository questionRepository,
        IQuestionOptionRepository optionRepository,
        ILogger<AnswerService> logger)
    {
        _answerRepository = answerRepository;
        _attemptRepository = attemptRepository;
        _examRepository = examRepository;
        _examSettingsRepository = examSettingsRepository;
        _examQuestionRepository = examQuestionRepository;
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AnswerResponse? Data)> SubmitAnswerAsync(long attemptId, SubmitAnswerRequest request)
    {
        try
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            if (attempt.Status != "IN_PROGRESS")
                return (false, "Exam attempt is not in progress", null);

            var (timeValid, timeError) = await ValidateAttemptTimeWindowAsync(attempt);
            if (!timeValid)
                return (false, timeError!, null);

            var existing = await _answerRepository.GetByAttemptAndQuestionAsync(attemptId, request.QuestionId);
            if (existing != null)
                return await UpdateAnswerAsync(attemptId, request.QuestionId, request);

            var answer = new Answer
            {
                ExamAttemptId = attemptId,
                QuestionId = request.QuestionId,
                TextContent = request.TextContent,
                EssayContent = request.EssayContent,
                CanvasImage = request.CanvasImage,
                AnsweredAt = DateTime.UtcNow
            };

            await _answerRepository.CreateAsync(answer);

            if (request.SelectedOptionIds?.Any() == true)
            {
                foreach (var optionId in request.SelectedOptionIds)
                {
                    await _answerRepository.CreateAnswerOptionAsync(new AnswerOption
                    {
                        AnswerId = answer.Id,
                        OptionId = optionId
                    });
                }
            }

            var fresh = await _answerRepository.GetByAttemptAndQuestionAsync(attemptId, request.QuestionId);
            return (true, "Answer submitted", MapToResponse(fresh!));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex,
                "Concurrent answer insert detected for attempt {AttemptId}, question {QuestionId}; retrying as update",
                attemptId, request.QuestionId);

            var existing = await _answerRepository.GetByAttemptAndQuestionAsync(attemptId, request.QuestionId);
            if (existing == null)
                return (false, "Concurrent submit conflict. Please retry.", null);

            return await UpdateAnswerAsync(attemptId, request.QuestionId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting answer for attempt {AttemptId}", attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, AnswerResponse? Data)> UpdateAnswerAsync(long attemptId, long questionId, SubmitAnswerRequest request)
    {
        try
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            if (attempt.Status != "IN_PROGRESS")
                return (false, "Exam attempt is not in progress", null);

            var (timeValid, timeError) = await ValidateAttemptTimeWindowAsync(attempt);
            if (!timeValid)
                return (false, timeError!, null);

            var answer = await _answerRepository.GetByAttemptAndQuestionAsync(attemptId, questionId);
            if (answer == null)
                return (false, "Answer not found", null);

            answer.TextContent = request.TextContent;
            answer.EssayContent = request.EssayContent;
            answer.CanvasImage = request.CanvasImage;
            answer.AnsweredAt = DateTime.UtcNow;

            await _answerRepository.UpdateAsync(answer);

            // Replace selected options
            await _answerRepository.DeleteAnswerOptionsByAnswerIdAsync(answer.Id);
            if (request.SelectedOptionIds?.Any() == true)
            {
                foreach (var optionId in request.SelectedOptionIds)
                {
                    await _answerRepository.CreateAnswerOptionAsync(new AnswerOption
                    {
                        AnswerId = answer.Id,
                        OptionId = optionId
                    });
                }
            }

            var fresh = await _answerRepository.GetByAttemptAndQuestionAsync(attemptId, questionId);
            return (true, "Answer updated", MapToResponse(fresh!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating answer for attempt {AttemptId}", attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, AnswerResponse? Data)> GetAnswerAsync(long attemptId, long questionId)
    {
        try
        {
            var answer = await _answerRepository.GetByAttemptAndQuestionAsync(attemptId, questionId);
            if (answer == null)
                return (false, "Answer not found", null);

            return (true, "Success", MapToResponse(answer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting answer");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<AttemptQuestionResponse>? Data)> GetAttemptQuestionsAsync(long attemptId)
    {
        try
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            var examQuestions = await _examQuestionRepository.GetExamQuestionsAsync(attempt.ExamId);
            var answers = await _answerRepository.GetByAttemptIdAsync(attemptId);
            var answerMap = answers.ToDictionary(a => a.QuestionId);

            var result = new List<AttemptQuestionResponse>();
            foreach (var eq in examQuestions.OrderBy(q => q.QuestionOrder))
            {
                var question = await _questionRepository.GetByIdAsync(eq.QuestionId);
                if (question == null) continue;

                var options = await _optionRepository.GetByQuestionIdAsync(question.Id);
                answerMap.TryGetValue(question.Id, out var answer);

                result.Add(new AttemptQuestionResponse
                {
                    QuestionId = question.Id,
                    Content = question.Content,
                    QuestionType = question.QuestionType?.Name ?? string.Empty,
                    OrderIndex = eq.QuestionOrder,
                    Points = eq.MaxScore,
                    Options = options.OrderBy(o => o.OrderIndex).Select(o => new AttemptQuestionOptionResponse
                    {
                        Id = o.Id,
                        Content = o.Content,
                        OrderIndex = o.OrderIndex
                    }).ToList(),
                    IsAnswered = answer != null,
                    CurrentAnswer = answer != null ? MapToResponse(answer) : null
                });
            }

            return (true, "Success", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attempt questions for attempt {AttemptId}", attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, AnswerResponse? Data)> AutoSaveAsync(long attemptId, SubmitAnswerRequest request)
    {
        // AutoSave behaves like SubmitAnswer but doesn't validate attempt status strictly
        try
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
                return (false, "Exam attempt not found", null);

            if (attempt.Status != "IN_PROGRESS")
                return (false, "Exam attempt is not in progress", null);

            return await SubmitAnswerAsync(attemptId, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-saving answer for attempt {AttemptId}", attemptId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    private static AnswerResponse MapToResponse(Answer answer) => new()
    {
        Id = answer.Id,
        ExamAttemptId = answer.ExamAttemptId,
        QuestionId = answer.QuestionId,
        SelectedOptionIds = answer.AnswerOptions?.Select(ao => ao.OptionId).ToList() ?? new(),
        TextContent = answer.TextContent,
        EssayContent = answer.EssayContent,
        CanvasImage = answer.CanvasImage,
        AnsweredAt = answer.AnsweredAt
    };

    private async Task<(bool Valid, string? Error)> ValidateAttemptTimeWindowAsync(ExamAttempt attempt)
    {
        var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
        if (exam == null)
            return (false, "Exam not found");

        var settings = await _examSettingsRepository.GetByExamIdAsync(attempt.ExamId);

        var now = DateTime.UtcNow;
        if (exam.StartTime != default && now < exam.StartTime)
            return (false, "Exam has not started yet");

        var hardDeadline = GetHardDeadline(exam, attempt.StartTime);
        if (now <= hardDeadline)
            return (true, null);

        if (settings?.AllowLateSubmission != true)
            return (false, "Exam duration exceeded");

        var gracePeriodMinutes = Math.Max(0, settings.GracePeriodMinutes);
        var finalDeadline = hardDeadline.AddMinutes(gracePeriodMinutes);
        if (now > finalDeadline)
            return (false, "Late submission window has closed");

        return (true, null);
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
}
