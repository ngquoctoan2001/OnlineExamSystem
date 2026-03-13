using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly ApplicationDbContext _context;

    public QuestionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Question?> GetByIdAsync(long id)
    {
        return await _context.Questions
            .AsNoTracking()
            .Include(q => q.Subject)
            .Include(q => q.QuestionType)
            .Include(q => q.QuestionOptions.OrderBy(o => o.OrderIndex))
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<(List<Question> Questions, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var totalCount = await _context.Questions.CountAsync();
        var questions = await _context.Questions
            .AsNoTracking()
            .Include(q => q.Subject)
            .Include(q => q.QuestionType)
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (questions, totalCount);
    }

    public async Task<List<Question>> SearchAsync(string searchTerm)
    {
        return await _context.Questions
            .AsNoTracking()
            .Include(q => q.Subject)
            .Include(q => q.QuestionType)
            .Where(q => q.Content.Contains(searchTerm))
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Question>> GetBySubjectAsync(long subjectId)
    {
        return await _context.Questions
            .AsNoTracking()
            .Include(q => q.Subject)
            .Include(q => q.QuestionType)
            .Where(q => q.SubjectId == subjectId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Question>> GetByDifficultyAsync(string difficulty)
    {
        return await _context.Questions
            .AsNoTracking()
            .Include(q => q.Subject)
            .Include(q => q.QuestionType)
            .Where(q => q.Difficulty == difficulty)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Question>> GetPublishedAsync(int page = 1, int pageSize = 20)
    {
        return await _context.Questions
            .AsNoTracking()
            .Include(q => q.Subject)
            .Include(q => q.QuestionType)
            .Where(q => q.IsPublished)
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Question>> GetByQuestionTypeAsync(long questionTypeId)
    {
        return await _context.Questions
            .AsNoTracking()
            .Include(q => q.Subject)
            .Include(q => q.QuestionType)
            .Where(q => q.QuestionTypeId == questionTypeId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Question>> GetByTeacherAsync(long teacherId)
    {
        return await _context.Questions
            .AsNoTracking()
            .Include(q => q.Subject)
            .Include(q => q.QuestionType)
            .Where(q => q.CreatedBy == teacherId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Question>> GetByTeacherAndSubjectAsync(long teacherId, long subjectId)
    {
        return await _context.Questions
            .AsNoTracking()
            .Include(q => q.Subject)
            .Include(q => q.QuestionType)
            .Where(q => q.CreatedBy == teacherId && q.SubjectId == subjectId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<Question> CreateAsync(Question question)
    {
        _context.Questions.Add(question);
        await _context.SaveChangesAsync();
        return question;
    }

    public async Task<Question> UpdateAsync(Question question)
    {
        _context.Questions.Update(question);
        await _context.SaveChangesAsync();
        return question;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question == null)
            return false;

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Questions.CountAsync();
    }

    public async Task<int> GetPublishedCountAsync()
    {
        return await _context.Questions.CountAsync(q => q.IsPublished);
    }
}
