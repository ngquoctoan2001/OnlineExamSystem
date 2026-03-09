using OnlineExamSystem.Application.DTOs;

namespace OnlineExamSystem.Infrastructure.Services;

public interface IExamAttemptService
{
    Task<(bool Success, string Message, ExamAttemptResponse? Data)> StartAttemptAsync(long examId, long studentId);
    Task<(bool Success, string Message, ExamAttemptResponse? Data)> GetAttemptByIdAsync(long attemptId);
    Task<(bool Success, string Message, ExamAttemptResponse? Data)> GetCurrentAttemptAsync(long studentId, long examId);
    Task<(bool Success, string Message, List<ExamAttemptResponse>? Data)> GetStudentAttemptsAsync(long studentId);
    Task<(bool Success, string Message, ExamAttemptListResponse? Data)> GetExamAttemptsAsync(long examId, int page = 1, int pageSize = 20);
    Task<(bool Success, string Message, SubmitExamAttemptResponse? Data)> SubmitAttemptAsync(long attemptId);
    Task<(bool Success, string Message, ExamAttemptDetailResponse? Data)> GetAttemptDetailAsync(long attemptId);
    Task<(bool Success, string Message, ExamAttemptListResponse? Data)> GetAllAttemptsAsync(int page = 1, int pageSize = 20);
}
