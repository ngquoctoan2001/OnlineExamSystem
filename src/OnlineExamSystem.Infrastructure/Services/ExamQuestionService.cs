using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

public class ExamQuestionService : IExamQuestionService
{
    private readonly IExamQuestionRepository _examQuestionRepository;
    private readonly IExamRepository _examRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionOptionRepository _optionRepository;
    private readonly ILogger<ExamQuestionService> _logger;

    public ExamQuestionService(
        IExamQuestionRepository examQuestionRepository,
        IExamRepository examRepository,
        IQuestionRepository questionRepository,
        IQuestionOptionRepository optionRepository,
        ILogger<ExamQuestionService> logger)
    {
        _examQuestionRepository = examQuestionRepository;
        _examRepository = examRepository;
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ExamQuestionDetailResponse? Data)> AddQuestionToExamAsync(AddQuestionToExamRequest request)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(request.ExamId);
            if (exam == null)
                return (false, "Exam not found", null);

            var question = await _questionRepository.GetByIdAsync(request.QuestionId);
            if (question == null)
                return (false, "Question not found", null);

            var exists = await _examQuestionRepository.ExistsAsync(request.ExamId, request.QuestionId);
            if (exists)
                return (false, "Question already added to this exam", null);

            var examQuestion = new ExamQuestion
            {
                ExamId = request.ExamId,
                QuestionId = request.QuestionId,
                QuestionOrder = request.QuestionOrder,
                MaxScore = request.MaxScore,
                AddedAt = DateTime.UtcNow
            };

            await _examQuestionRepository.CreateAsync(examQuestion);

            return (true, "Question added to exam successfully", await MapToDetailResponseAsync(examQuestion));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding question to exam");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamQuestionsListResponse? Data)> GetExamQuestionsAsync(long examId)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found", null);

            var examQuestions = await _examQuestionRepository.GetExamQuestionsAsync(examId);
            var questions = new List<ExamQuestionResponse>();
            int totalScore = 0;

            foreach (var eq in examQuestions)
            {
                questions.Add(new ExamQuestionResponse
                {
                    Id = eq.Id,
                    ExamId = eq.ExamId,
                    QuestionId = eq.QuestionId,
                    QuestionContent = eq.Question?.Content,
                    QuestionDifficulty = eq.Question?.Difficulty,
                    QuestionOrder = eq.QuestionOrder,
                    MaxScore = eq.MaxScore,
                    OptionCount = eq.Question?.QuestionOptions?.Count ?? 0,
                    AddedAt = eq.AddedAt
                });
                totalScore += eq.MaxScore;
            }

            return (true, "Success", new ExamQuestionsListResponse
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                Questions = questions,
                TotalQuestions = questions.Count,
                TotalScore = totalScore
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam questions");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ExamQuestionResponse>? Data)> GetAllQuestionsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var examQuestions = await _examQuestionRepository.GetAllAsync(page, pageSize);
            var questions = examQuestions.Select(eq => new ExamQuestionResponse
            {
                Id = eq.Id,
                ExamId = eq.ExamId,
                QuestionId = eq.QuestionId,
                QuestionContent = eq.Question?.Content,
                QuestionDifficulty = eq.Question?.Difficulty,
                QuestionOrder = eq.QuestionOrder,
                MaxScore = eq.MaxScore,
                OptionCount = eq.Question?.QuestionOptions?.Count ?? 0,
                AddedAt = eq.AddedAt
            }).ToList();

            return (true, "Success", questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all exam questions");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamQuestionDetailResponse? Data)> GetExamQuestionByIdAsync(long examQuestionId)
    {
        try
        {
            var examQuestion = await _examQuestionRepository.GetByIdAsync(examQuestionId);
            if (examQuestion == null)
                return (false, "Exam question not found", null);

            return (true, "Success", await MapToDetailResponseAsync(examQuestion));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam question");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> RemoveQuestionFromExamAsync(long examId, long questionId)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found");

            var success = await _examQuestionRepository.DeleteExamQuestionAsync(examId, questionId);
            if (!success)
                return (false, "Question not found in exam");

            return (true, "Question removed from exam successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing question from exam");
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ReorderQuestionsAsync(long examId, ReorderExamQuestionsRequest request)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
                return (false, "Exam not found");

            var examQuestions = await _examQuestionRepository.GetExamQuestionsAsync(examId);

            foreach (var orderItem in request.Questions)
            {
                var examQuestion = examQuestions.FirstOrDefault(eq => eq.Id == orderItem.ExamQuestionId);
                if (examQuestion != null)
                {
                    examQuestion.QuestionOrder = orderItem.NewOrder;
                    await _examQuestionRepository.UpdateAsync(examQuestion);
                }
            }

            return (true, "Questions reordered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering questions");
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UpdateQuestionMaxScoreAsync(long examQuestionId, int maxScore)
    {
        try
        {
            var examQuestion = await _examQuestionRepository.GetByIdAsync(examQuestionId);
            if (examQuestion == null)
                return (false, "Exam question not found");

            if (maxScore <= 0)
                return (false, "Max score must be greater than zero");

            examQuestion.MaxScore = maxScore;
            await _examQuestionRepository.UpdateAsync(examQuestion);

            return (true, "Max score updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating max score");
            return (false, $"Error: {ex.Message}");
        }
    }

    private async Task<ExamQuestionDetailResponse> MapToDetailResponseAsync(ExamQuestion examQuestion)
    {
        var options = await _optionRepository.GetByQuestionIdAsync(examQuestion.QuestionId);
        
        return new ExamQuestionDetailResponse
        {
            Id = examQuestion.Id,
            ExamId = examQuestion.ExamId,
            QuestionId = examQuestion.QuestionId,
            QuestionContent = examQuestion.Question?.Content,
            QuestionDifficulty = examQuestion.Question?.Difficulty,
            QuestionOrder = examQuestion.QuestionOrder,
            MaxScore = examQuestion.MaxScore,
            AddedAt = examQuestion.AddedAt,
            Options = options.Select(o => new QuestionOptionResponse
            {
                Id = o.Id,
                QuestionId = o.QuestionId,
                Label = o.Label,
                Content = o.Content,
                IsCorrect = o.IsCorrect,
                OrderIndex = o.OrderIndex
            }).ToList()
        };
    }
}
