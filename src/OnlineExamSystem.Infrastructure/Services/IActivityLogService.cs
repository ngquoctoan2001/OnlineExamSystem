using OnlineExamSystem.Application.DTOs;

namespace OnlineExamSystem.Infrastructure.Services;

public interface IActivityLogService
{
    Task LogAsync(long? userId, string action, string? entityType = null, long? entityId = null,
        string? detail = null, string? ipAddress = null);
    Task<(bool Success, string Message, ActivityLogPagedResponse? Data)> GetLogsAsync(
        int page, int pageSize, string? action = null, long? userId = null,
        DateTime? from = null, DateTime? to = null);
}
