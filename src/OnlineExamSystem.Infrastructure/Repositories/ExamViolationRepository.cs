using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class ExamViolationRepository : IExamViolationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExamViolationRepository> _logger;

    public ExamViolationRepository(ApplicationDbContext context, ILogger<ExamViolationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ExamViolation>> GetByAttemptIdAsync(long attemptId)
    {
        return await _context.ExamViolations
            .Where(v => v.ExamAttemptId == attemptId)
            .OrderBy(v => v.OccurredAt)
            .ToListAsync();
    }

    public async Task<ExamViolation> CreateAsync(ExamViolation violation)
    {
        _context.ExamViolations.Add(violation);
        await _context.SaveChangesAsync();
        return violation;
    }
}
