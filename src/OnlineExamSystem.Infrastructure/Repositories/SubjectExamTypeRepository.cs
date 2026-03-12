using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

namespace OnlineExamSystem.Infrastructure.Repositories;

public class SubjectExamTypeRepository : ISubjectExamTypeRepository
{
    private readonly ApplicationDbContext _context;

    public SubjectExamTypeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SubjectExamType?> GetByIdAsync(long id)
    {
        return await _context.SubjectExamTypes
            .AsNoTracking()
            .Include(e => e.Subject)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<SubjectExamType>> GetBySubjectIdAsync(long subjectId)
    {
        return await _context.SubjectExamTypes
            .AsNoTracking()
            .Include(e => e.Subject)
            .Where(e => e.SubjectId == subjectId)
            .OrderBy(e => e.SortOrder)
            .ToListAsync();
    }

    public async Task<SubjectExamType> CreateAsync(SubjectExamType entity)
    {
        _context.SubjectExamTypes.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<SubjectExamType> UpdateAsync(SubjectExamType entity)
    {
        _context.SubjectExamTypes.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var entity = await _context.SubjectExamTypes.FindAsync(id);
        if (entity == null) return false;
        _context.SubjectExamTypes.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}
