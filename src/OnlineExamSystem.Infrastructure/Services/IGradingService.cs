using OnlineExamSystem.Application.DTOs;

namespace OnlineExamSystem.Infrastructure.Services;

public interface IGradingService
{
    Task<(bool Success, string Message, List<GradingResultResponse>? Data)> AutoGradeAttemptAsync(long attemptId);
    Task<(bool Success, string Message, AttemptGradingViewResponse? Data)> GetAttemptGradingViewAsync(long attemptId);
    Task<(bool Success, string Message, List<PendingGradingAttemptResponse>? Data)> GetPendingGradingAsync(long examId);
    Task<(bool Success, string Message, GradingResultResponse? Data)> ManualGradeQuestionAsync(long attemptId, long questionId, ManualGradeRequest request, long gradedBy);
    Task<(bool Success, string Message)> MarkAsGradedAsync(long attemptId);
    Task<(bool Success, string Message, PublishResultResponse? Data)> PublishResultAsync(long attemptId);
    Task<(bool Success, string Message, AttemptGradingViewResponse? Data)> GetStudentResultAsync(long attemptId);
}
