using OnlineExamSystem.Application.DTOs;

namespace OnlineExamSystem.Infrastructure.Services;

public interface IAnswerService
{
    Task<(bool Success, string Message, AnswerResponse? Data)> SubmitAnswerAsync(long attemptId, SubmitAnswerRequest request);
    Task<(bool Success, string Message, AnswerResponse? Data)> UpdateAnswerAsync(long attemptId, long questionId, SubmitAnswerRequest request);
    Task<(bool Success, string Message, AnswerResponse? Data)> GetAnswerAsync(long attemptId, long questionId);
    Task<(bool Success, string Message, List<AttemptQuestionResponse>? Data)> GetAttemptQuestionsAsync(long attemptId);
    Task<(bool Success, string Message, AnswerResponse? Data)> AutoSaveAsync(long attemptId, SubmitAnswerRequest request);
}
