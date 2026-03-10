using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class ExamQuestionRepository : IExamQuestionRepository
{
    private readonly ApplicationDbContext _context;

    public ExamQuestionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExamQuestion?> GetByIdAsync(long id)
    {
        return await _context.ExamQuestions
            .AsNoTracking()
            .Include(eq => eq.Exam)
            .Include(eq => eq.Question)
            .ThenInclude(q => q.QuestionOptions)
            .FirstOrDefaultAsync(eq => eq.Id == id);
    }

    public async Task<List<ExamQuestion>> GetExamQuestionsAsync(long examId)
    {
        return await _context.ExamQuestions
            .AsNoTracking()
            .Include(eq => eq.Question)
            .ThenInclude(q => q.QuestionType)
            .Include(eq => eq.Question)
            .ThenInclude(q => q.QuestionOptions)
            .Where(eq => eq.ExamId == examId)
            .OrderBy(eq => eq.QuestionOrder)
            .ToListAsync();
    }

    public async Task<List<ExamQuestion>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _context.ExamQuestions
            .AsNoTracking()
            .Include(eq => eq.Exam)
            .Include(eq => eq.Question)
            .OrderBy(eq => eq.ExamId)
            .ThenBy(eq => eq.QuestionOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<ExamQuestion?> GetExamQuestionAsync(long examId, long questionId)
    {
        return await _context.ExamQuestions
            .AsNoTracking()
            .FirstOrDefaultAsync(eq => eq.ExamId == examId && eq.QuestionId == questionId);
    }

    public async Task<ExamQuestion> CreateAsync(ExamQuestion examQuestion)
    {
        _context.ExamQuestions.Add(examQuestion);
        await _context.SaveChangesAsync();
        return examQuestion;
    }

    public async Task<ExamQuestion> UpdateAsync(ExamQuestion examQuestion)
    {
        _context.ExamQuestions.Update(examQuestion);
        await _context.SaveChangesAsync();
        return examQuestion;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var examQuestion = await _context.ExamQuestions.FindAsync(id);
        if (examQuestion == null)
            return false;

        _context.ExamQuestions.Remove(examQuestion);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteExamQuestionAsync(long examId, long questionId)
    {
        var examQuestion = await _context.ExamQuestions
            .FirstOrDefaultAsync(eq => eq.ExamId == examId && eq.QuestionId == questionId);
        
        if (examQuestion == null)
            return false;

        _context.ExamQuestions.Remove(examQuestion);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetQuestionCountForExamAsync(long examId)
    {
        return await _context.ExamQuestions.CountAsync(eq => eq.ExamId == examId);
    }

    public async Task<bool> ExistsAsync(long examId, long questionId)
    {
        return await _context.ExamQuestions
            .AnyAsync(eq => eq.ExamId == examId && eq.QuestionId == questionId);
    }

    public async Task<int> GetMaxOrderAsync(long examId)
    {
        var maxOrder = await _context.ExamQuestions
            .Where(eq => eq.ExamId == examId)
            .MaxAsync(eq => (int?)eq.QuestionOrder);
        
        return maxOrder ?? 0;
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.ExamQuestions.CountAsync();
    }
}
