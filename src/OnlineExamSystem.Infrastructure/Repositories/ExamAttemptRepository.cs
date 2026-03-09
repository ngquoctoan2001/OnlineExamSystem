using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class ExamAttemptRepository : IExamAttemptRepository
{
    private readonly ApplicationDbContext _context;

    public ExamAttemptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExamAttempt?> GetByIdAsync(long id)
    {
        return await _context.ExamAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(ea => ea.Id == id);
    }

    public async Task<(List<ExamAttempt> Attempts, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var totalCount = await _context.ExamAttempts.CountAsync();
        var attempts = await _context.ExamAttempts
            .AsNoTracking()
            .OrderByDescending(ea => ea.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (attempts, totalCount);
    }

    public async Task<List<ExamAttempt>> GetStudentAttemptsAsync(long studentId)
    {
        return await _context.ExamAttempts
            .AsNoTracking()
            .Where(ea => ea.StudentId == studentId)
            .OrderByDescending(ea => ea.StartTime)
            .ToListAsync();
    }

    public async Task<List<ExamAttempt>> GetExamAttemptsAsync(long examId)
    {
        return await _context.ExamAttempts
            .AsNoTracking()
            .Where(ea => ea.ExamId == examId)
            .OrderByDescending(ea => ea.StartTime)
            .ToListAsync();
    }

    public async Task<ExamAttempt?> GetStudentExamAttemptAsync(long studentId, long examId)
    {
        return await _context.ExamAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(ea => ea.StudentId == studentId && ea.ExamId == examId && ea.Status == "IN_PROGRESS");
    }

    public async Task<ExamAttempt> CreateAsync(ExamAttempt attempt)
    {
        _context.ExamAttempts.Add(attempt);
        await _context.SaveChangesAsync();
        return attempt;
    }

    public async Task<ExamAttempt> UpdateAsync(ExamAttempt attempt)
    {
        _context.ExamAttempts.Update(attempt);
        await _context.SaveChangesAsync();
        return attempt;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var attempt = await _context.ExamAttempts.FindAsync(id);
        if (attempt == null)
            return false;

        _context.ExamAttempts.Remove(attempt);
        await _context.SaveChangesAsync();
        return true;
    }
}
