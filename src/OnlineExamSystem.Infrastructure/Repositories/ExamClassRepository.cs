using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class ExamClassRepository : IExamClassRepository
{
    private readonly ApplicationDbContext _context;

    public ExamClassRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExamClass?> GetByIdAsync(long examId, long classId)
    {
        return await _context.ExamClasses
            .AsNoTracking()
            .Include(ec => ec.Exam)
            .Include(ec => ec.Class)
            .FirstOrDefaultAsync(ec => ec.ExamId == examId && ec.ClassId == classId);
    }

    public async Task<List<ExamClass>> GetExamClassesAsync(long examId)
    {
        return await _context.ExamClasses
            .AsNoTracking()
            .Include(ec => ec.Class)
            .Where(ec => ec.ExamId == examId)
            .OrderBy(ec => ec.Class.Name)
            .ToListAsync();
    }

    public async Task<List<ExamClass>> GetClassExamsAsync(long classId)
    {
        return await _context.ExamClasses
            .AsNoTracking()
            .Include(ec => ec.Exam)
            .Where(ec => ec.ClassId == classId)
            .OrderByDescending(ec => ec.Exam.StartTime)
            .ToListAsync();
    }

    public async Task<(List<ExamClass> ExamClasses, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var query = _context.ExamClasses
            .AsNoTracking()
            .Include(ec => ec.Exam)
            .Include(ec => ec.Class)
            .OrderByDescending(ec => ec.Exam.CreatedAt);

        var totalCount = await query.CountAsync();
        var examClasses = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (examClasses, totalCount);
    }

    public async Task<ExamClass> CreateAsync(ExamClass examClass)
    {
        _context.ExamClasses.Add(examClass);
        await _context.SaveChangesAsync();
        return examClass;
    }

    public async Task<bool> DeleteAsync(long examId, long classId)
    {
        var examClass = await _context.ExamClasses
            .FirstOrDefaultAsync(ec => ec.ExamId == examId && ec.ClassId == classId);

        if (examClass == null)
            return false;

        _context.ExamClasses.Remove(examClass);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(long examId, long classId)
    {
        return await _context.ExamClasses
            .AnyAsync(ec => ec.ExamId == examId && ec.ClassId == classId);
    }

    public async Task<int> GetClassCountForExamAsync(long examId)
    {
        return await _context.ExamClasses
            .CountAsync(ec => ec.ExamId == examId);
    }

    public async Task<int> GetExamCountForClassAsync(long classId)
    {
        return await _context.ExamClasses
            .CountAsync(ec => ec.ClassId == classId);
    }
}
