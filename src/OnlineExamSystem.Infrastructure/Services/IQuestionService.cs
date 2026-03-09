using OnlineExamSystem.Application.DTOs;

namespace OnlineExamSystem.Infrastructure.Services;

public interface IQuestionService
{
    Task<(bool Success, string Message, QuestionDetailResponse? Data)> CreateQuestionAsync(CreateQuestionRequest request, long createdBy);
    Task<(bool Success, string Message, QuestionDetailResponse? Data)> GetQuestionByIdAsync(long questionId);
    Task<(bool Success, string Message, QuestionListResponse? Data)> GetAllQuestionsAsync(int page = 1, int pageSize = 20);
    Task<(bool Success, string Message, QuestionListResponse? Data)> GetPublishedQuestionsAsync(int page = 1, int pageSize = 20);
    Task<(bool Success, string Message, List<QuestionResponse>? Data)> SearchQuestionsAsync(string searchTerm);
    Task<(bool Success, string Message, List<QuestionResponse>? Data)> GetQuestionsBySubjectAsync(long subjectId);
    Task<(bool Success, string Message, List<QuestionResponse>? Data)> GetQuestionsByDifficultyAsync(string difficulty);
    Task<(bool Success, string Message, List<QuestionResponse>? Data)> GetQuestionsByTypeAsync(long questionTypeId);
    Task<(bool Success, string Message, QuestionDetailResponse? Data)> UpdateQuestionAsync(long questionId, UpdateQuestionRequest request);
    Task<(bool Success, string Message)> PublishQuestionAsync(long questionId);
    Task<(bool Success, string Message)> UnpublishQuestionAsync(long questionId);
    Task<(bool Success, string Message)> DeleteQuestionAsync(long questionId);
}
