using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

public class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionOptionRepository _optionRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<QuestionService> _logger;

    public QuestionService(
        IQuestionRepository questionRepository,
        IQuestionOptionRepository optionRepository,
        ISubjectRepository subjectRepository,
        ILogger<QuestionService> logger)
    {
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, QuestionDetailResponse? Data)> CreateQuestionAsync(CreateQuestionRequest request, long createdBy)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(request.SubjectId);
            if (subject == null)
                return (false, "Subject not found", null);

            var question = new Question
            {
                SubjectId = request.SubjectId,
                QuestionTypeId = request.QuestionTypeId,
                CreatedBy = createdBy,
                Content = request.Content,
                Difficulty = request.Difficulty,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };

            await _questionRepository.CreateAsync(question);

            if (request.Options?.Any() == true)
            {
                var options = request.Options.Select(opt => new QuestionOption
                {
                    QuestionId = question.Id,
                    Label = opt.Label,
                    Content = opt.Content,
                    IsCorrect = opt.IsCorrect,
                    OrderIndex = opt.OrderIndex
                }).ToList();

                await _optionRepository.CreateBatchAsync(options);
            }

            return (true, "Question created successfully", await GetQuestionDetailResponseAsync(question));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, QuestionDetailResponse? Data)> GetQuestionByIdAsync(long questionId)
    {
        try
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            if (question == null)
                return (false, "Question not found", null);

            return (true, "Success", await GetQuestionDetailResponseAsync(question));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, QuestionListResponse? Data)> GetAllQuestionsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (questions, totalCount) = await _questionRepository.GetAllAsync(page, pageSize);
            var items = await MapToResponseListAsync(questions);

            return (true, "Success", new QuestionListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all questions");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, QuestionListResponse? Data)> GetPublishedQuestionsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var questions = await _questionRepository.GetPublishedAsync(page, pageSize);
            var items = await MapToResponseListAsync(questions);
            var totalCount = await _questionRepository.GetPublishedCountAsync();

            return (true, "Success", new QuestionListResponse
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting published questions");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<QuestionResponse>? Data)> SearchQuestionsAsync(string searchTerm)
    {
        try
        {
            var questions = await _questionRepository.SearchAsync(searchTerm);
            var items = await MapToResponseListAsync(questions);
            return (true, "Success", items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching questions");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<QuestionResponse>? Data)> GetQuestionsBySubjectAsync(long subjectId)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(subjectId);
            if (subject == null)
                return (false, "Subject not found", null);

            var questions = await _questionRepository.GetBySubjectAsync(subjectId);
            var items = await MapToResponseListAsync(questions);
            return (true, "Success", items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions by subject");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<QuestionResponse>? Data)> GetQuestionsByDifficultyAsync(string difficulty)
    {
        try
        {
            if (!new[] { "EASY", "MEDIUM", "HARD" }.Contains(difficulty.ToUpper()))
                return (false, "Invalid difficulty level", null);

            var questions = await _questionRepository.GetByDifficultyAsync(difficulty.ToUpper());
            var items = await MapToResponseListAsync(questions);
            return (true, "Success", items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions by difficulty");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<QuestionResponse>? Data)> GetQuestionsByTypeAsync(long questionTypeId)
    {
        try
        {
            var questions = await _questionRepository.GetByQuestionTypeAsync(questionTypeId);
            var items = await MapToResponseListAsync(questions);
            return (true, "Success", items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions by type");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, QuestionDetailResponse? Data)> UpdateQuestionAsync(long questionId, UpdateQuestionRequest request)
    {
        try
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            if (question == null)
                return (false, "Question not found", null);

            question.Content = request.Content;
            question.Difficulty = request.Difficulty;
            question.IsPublished = request.IsPublished;

            await _questionRepository.UpdateAsync(question);

            if (request.Options?.Any() == true)
            {
                await _optionRepository.DeleteByQuestionIdAsync(questionId);
                var options = request.Options.Select(opt => new QuestionOption
                {
                    QuestionId = questionId,
                    Label = opt.Label,
                    Content = opt.Content,
                    IsCorrect = opt.IsCorrect,
                    OrderIndex = opt.OrderIndex
                }).ToList();

                await _optionRepository.CreateBatchAsync(options);
            }

            return (true, "Question updated successfully", await GetQuestionDetailResponseAsync(question));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> PublishQuestionAsync(long questionId)
    {
        try
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            if (question == null)
                return (false, "Question not found");

            question.IsPublished = true;
            await _questionRepository.UpdateAsync(question);

            return (true, "Question published successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing question");
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> UnpublishQuestionAsync(long questionId)
    {
        try
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            if (question == null)
                return (false, "Question not found");

            question.IsPublished = false;
            await _questionRepository.UpdateAsync(question);

            return (true, "Question unpublished successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing question");
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteQuestionAsync(long questionId)
    {
        try
        {
            var question = await _questionRepository.GetByIdAsync(questionId);
            if (question == null)
                return (false, "Question not found");

            await _optionRepository.DeleteByQuestionIdAsync(questionId);
            await _questionRepository.DeleteAsync(questionId);

            return (true, "Question deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question");
            return (false, $"Error: {ex.Message}");
        }
    }

    private async Task<QuestionDetailResponse> GetQuestionDetailResponseAsync(Question question)
    {
        var options = await _optionRepository.GetByQuestionIdAsync(question.Id);
        return new QuestionDetailResponse
        {
            Id = question.Id,
            SubjectId = question.SubjectId,
            SubjectName = question.Subject?.Name,
            QuestionTypeId = question.QuestionTypeId,
            QuestionTypeName = question.QuestionType?.Name,
            Content = question.Content,
            Difficulty = question.Difficulty,
            IsPublished = question.IsPublished,
            CreatedAt = question.CreatedAt,
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

    private async Task<List<QuestionResponse>> MapToResponseListAsync(List<Question> questions)
    {
        var responses = new List<QuestionResponse>();
        foreach (var question in questions)
        {
            var optionCount = await _optionRepository.GetCountByQuestionIdAsync(question.Id);
            responses.Add(new QuestionResponse
            {
                Id = question.Id,
                SubjectId = question.SubjectId,
                SubjectName = question.Subject?.Name,
                QuestionTypeId = question.QuestionTypeId,
                QuestionTypeName = question.QuestionType?.Name,
                Content = question.Content,
                Difficulty = question.Difficulty,
                IsPublished = question.IsPublished,
                CreatedAt = question.CreatedAt,
                OptionCount = optionCount
            });
        }
        return responses;
    }
}
