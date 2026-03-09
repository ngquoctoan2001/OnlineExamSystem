using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class QuestionOptionRepository : IQuestionOptionRepository
{
    private readonly ApplicationDbContext _context;

    public QuestionOptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QuestionOption?> GetByIdAsync(long id)
    {
        return await _context.QuestionOptions
            .AsNoTracking()
            .FirstOrDefaultAsync(qo => qo.Id == id);
    }

    public async Task<List<QuestionOption>> GetByQuestionIdAsync(long questionId)
    {
        return await _context.QuestionOptions
            .AsNoTracking()
            .Where(qo => qo.QuestionId == questionId)
            .OrderBy(qo => qo.OrderIndex)
            .ToListAsync();
    }

    public async Task<List<QuestionOption>> GetAllAsync()
    {
        return await _context.QuestionOptions
            .AsNoTracking()
            .OrderBy(qo => qo.QuestionId)
            .ThenBy(qo => qo.OrderIndex)
            .ToListAsync();
    }

    public async Task<QuestionOption?> GetCorrectOptionAsync(long questionId)
    {
        return await _context.QuestionOptions
            .AsNoTracking()
            .FirstOrDefaultAsync(qo => qo.QuestionId == questionId && qo.IsCorrect);
    }

    public async Task<List<QuestionOption>> GetCorrectOptionsAsync(List<long> questionIds)
    {
        return await _context.QuestionOptions
            .AsNoTracking()
            .Where(qo => questionIds.Contains(qo.QuestionId) && qo.IsCorrect)
            .ToListAsync();
    }

    public async Task<QuestionOption> CreateAsync(QuestionOption option)
    {
        _context.QuestionOptions.Add(option);
        await _context.SaveChangesAsync();
        return option;
    }

    public async Task<List<QuestionOption>> CreateBatchAsync(List<QuestionOption> options)
    {
        _context.QuestionOptions.AddRange(options);
        await _context.SaveChangesAsync();
        return options;
    }

    public async Task<QuestionOption> UpdateAsync(QuestionOption option)
    {
        _context.QuestionOptions.Update(option);
        await _context.SaveChangesAsync();
        return option;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var option = await _context.QuestionOptions.FindAsync(id);
        if (option == null)
            return false;

        _context.QuestionOptions.Remove(option);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByQuestionIdAsync(long questionId)
    {
        var options = await _context.QuestionOptions.Where(qo => qo.QuestionId == questionId).ToListAsync();
        if (!options.Any())
            return false;

        _context.QuestionOptions.RemoveRange(options);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCountByQuestionIdAsync(long questionId)
    {
        return await _context.QuestionOptions.CountAsync(qo => qo.QuestionId == questionId);
    }
}
