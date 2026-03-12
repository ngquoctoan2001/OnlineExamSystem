using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class ExamRepository : IExamRepository
{
    private readonly ApplicationDbContext _context;

    public ExamRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Exam?> GetByIdAsync(long id)
    {
        return await _context.Exams
            .AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.ExamQuestions)
            .Include(e => e.SubjectExamType)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<(List<Exam> Exams, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var query = _context.Exams
            .AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.SubjectExamType)
            .OrderByDescending(e => e.CreatedAt);

        var totalCount = await query.CountAsync();
        var exams = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (exams, totalCount);
    }

    public async Task<List<Exam>> SearchAsync(string searchTerm)
    {
        return await _context.Exams
            .AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.SubjectExamType)
            .Where(e => e.Title.Contains(searchTerm) || e.Description.Contains(searchTerm))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Exam>> GetByTeacherAsync(long teacherId)
    {
        return await _context.Exams
            .AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.SubjectExamType)
            .Where(e => e.CreatedBy == teacherId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Exam>> GetBySubjectAsync(long subjectId)
    {
        return await _context.Exams
            .AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.SubjectExamType)
            .Where(e => e.SubjectId == subjectId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<Exam> CreateAsync(Exam exam)
    {
        exam.CreatedAt = DateTime.UtcNow;
        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();
        return exam;
    }

    public async Task<Exam> UpdateAsync(Exam exam)
    {
        _context.Exams.Update(exam);
        await _context.SaveChangesAsync();
        return exam;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == id);
        if (exam == null) return false;

        // Don't allow deletion if exam is active or closed
        if (exam.Status != "DRAFT")
            return false;

        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TitleExistsAsync(string title, long? excludeExamId = null)
    {
        var query = _context.Exams.AsNoTracking().Where(e => e.Title.ToLower() == title.ToLower());
        if (excludeExamId.HasValue)
            query = query.Where(e => e.Id != excludeExamId.Value);
        return await query.AnyAsync();
    }
}
