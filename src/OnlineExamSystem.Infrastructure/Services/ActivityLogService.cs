using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _repo;

    public ActivityLogService(IActivityLogRepository repo)
    {
        _repo = repo;
    }

    public async Task LogAsync(long? userId, string action, string? entityType = null, long? entityId = null,
        string? detail = null, string? ipAddress = null)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail,
            IpAddress = ipAddress,
            OccurredAt = DateTime.UtcNow
        };
        await _repo.CreateAsync(log);
    }

    public async Task<(bool Success, string Message, ActivityLogPagedResponse? Data)> GetLogsAsync(
        int page, int pageSize, string? action = null, long? userId = null,
        DateTime? from = null, DateTime? to = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var (logs, totalCount) = await _repo.GetAllAsync(page, pageSize, action, userId, from, to);

        var response = new ActivityLogPagedResponse
        {
            Logs = logs.Select(l => new ActivityLogResponse
            {
                Id = l.Id,
                UserId = l.UserId,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Detail = l.Detail,
                IpAddress = l.IpAddress,
                OccurredAt = l.OccurredAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return (true, "OK", response);
    }
}
