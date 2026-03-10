using OnlineExamSystem.Application.DTOs;

namespace OnlineExamSystem.Infrastructure.Services;

public interface IStatisticsService
{
    Task<(bool Success, string Message, ExamStatisticResponse? Data)> CalculateAndSaveExamStatisticsAsync(long examId);
    Task<(bool Success, string Message, ExamStatisticResponse? Data)> GetExamStatisticsAsync(long examId);
    Task<(bool Success, string Message, ScoreDistributionResponse? Data)> GetScoreDistributionAsync(long examId);
    Task<(bool Success, string Message, StudentPerformanceResponse? Data)> GetStudentPerformanceAsync(long studentId);
    Task<(bool Success, string Message, ClassResultsResponse? Data)> GetClassResultsAsync(long classId, long examId);
}
