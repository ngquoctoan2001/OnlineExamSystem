using OnlineExamSystem.Application.DTOs;

namespace OnlineExamSystem.Infrastructure.Services;

public interface IExamQuestionService
{
    Task<(bool Success, string Message, ExamQuestionDetailResponse? Data)> AddQuestionToExamAsync(AddQuestionToExamRequest request);
    Task<(bool Success, string Message, ExamQuestionsListResponse? Data)> GetExamQuestionsAsync(long examId);
    Task<(bool Success, string Message, List<ExamQuestionResponse>? Data)> GetAllQuestionsAsync(int page = 1, int pageSize = 20);
    Task<(bool Success, string Message, ExamQuestionDetailResponse? Data)> GetExamQuestionByIdAsync(long examQuestionId);
    Task<(bool Success, string Message)> RemoveQuestionFromExamAsync(long examId, long questionId);
    Task<(bool Success, string Message)> ReorderQuestionsAsync(long examId, ReorderExamQuestionsRequest request);
    Task<(bool Success, string Message)> UpdateQuestionMaxScoreAsync(long examQuestionId, int maxScore);
}
