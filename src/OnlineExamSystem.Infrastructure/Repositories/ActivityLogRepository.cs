using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly ApplicationDbContext _context;

    public ActivityLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ActivityLog> CreateAsync(ActivityLog log)
    {
        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<(List<ActivityLog> Logs, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? action = null, long? userId = null,
        DateTime? from = null, DateTime? to = null)
    {
        var query = _context.ActivityLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);
        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId);
        if (from.HasValue)
            query = query.Where(l => l.OccurredAt >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.OccurredAt <= to.Value);

        var totalCount = await query.CountAsync();
        var logs = await query
            .OrderByDescending(l => l.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }
}
