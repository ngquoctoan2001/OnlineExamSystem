using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class GradingResultRepository : IGradingResultRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GradingResultRepository> _logger;

    public GradingResultRepository(ApplicationDbContext context, ILogger<GradingResultRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GradingResult?> GetByAttemptAndQuestionAsync(long attemptId, long questionId)
    {
        return await _context.GradingResults
            .FirstOrDefaultAsync(g => g.ExamAttemptId == attemptId && g.QuestionId == questionId);
    }

    public async Task<List<GradingResult>> GetByAttemptIdAsync(long attemptId)
    {
        return await _context.GradingResults
            .Where(g => g.ExamAttemptId == attemptId)
            .ToListAsync();
    }

    public async Task<GradingResult> CreateAsync(GradingResult result)
    {
        _context.GradingResults.Add(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<GradingResult> UpdateAsync(GradingResult result)
    {
        _context.GradingResults.Update(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<List<GradingResult>> GetByExamIdAsync(long examId)
    {
        return await _context.GradingResults
            .Include(g => g.ExamAttempt)
            .Where(g => g.ExamAttempt.ExamId == examId)
            .ToListAsync();
    }
}
