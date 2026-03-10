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
    Task<(bool Success, string Message, List<QuestionResponse>? Data)> GetQuestionsByTagAsync(long tagId);
    Task<(bool Success, string Message, QuestionDetailResponse? Data)> UpdateQuestionAsync(long questionId, UpdateQuestionRequest request);
    Task<(bool Success, string Message)> PublishQuestionAsync(long questionId);
    Task<(bool Success, string Message)> UnpublishQuestionAsync(long questionId);
    Task<(bool Success, string Message)> DeleteQuestionAsync(long questionId);

    // Option management
    Task<(bool Success, string Message, QuestionOptionResponse? Data)> AddOptionAsync(long questionId, CreateQuestionOptionRequest request);
    Task<(bool Success, string Message, QuestionOptionResponse? Data)> UpdateOptionAsync(long questionId, long optionId, CreateQuestionOptionRequest request);
    Task<(bool Success, string Message)> DeleteOptionAsync(long questionId, long optionId);

    // Tag management
    Task<(bool Success, string Message)> AssignTagAsync(long questionId, long tagId);
    Task<(bool Success, string Message)> RemoveTagAsync(long questionId, long tagId);
    Task<(bool Success, string Message, List<TagResponse>? Data)> GetQuestionTagsAsync(long questionId);
}
