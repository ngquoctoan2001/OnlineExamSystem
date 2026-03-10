using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class ExamStatisticRepository : IExamStatisticRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExamStatisticRepository> _logger;

    public ExamStatisticRepository(ApplicationDbContext context, ILogger<ExamStatisticRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExamStatistic?> GetByExamIdAsync(long examId)
    {
        return await _context.ExamStatistics
            .FirstOrDefaultAsync(s => s.ExamId == examId);
    }

    public async Task<ExamStatistic> CreateAsync(ExamStatistic stat)
    {
        _context.ExamStatistics.Add(stat);
        await _context.SaveChangesAsync();
        return stat;
    }

    public async Task<ExamStatistic> UpdateOrCreateAsync(ExamStatistic stat)
    {
        var existing = await _context.ExamStatistics
            .FirstOrDefaultAsync(s => s.ExamId == stat.ExamId);

        if (existing == null)
        {
            _context.ExamStatistics.Add(stat);
        }
        else
        {
            existing.TotalAttempts = stat.TotalAttempts;
            existing.PassCount = stat.PassCount;
            existing.FailCount = stat.FailCount;
            existing.AverageScore = stat.AverageScore;
            existing.MaxScore = stat.MaxScore;
            existing.MinScore = stat.MinScore;
            existing.CalculatedAt = stat.CalculatedAt;
            stat = existing;
        }

        await _context.SaveChangesAsync();
        return stat;
    }
}
