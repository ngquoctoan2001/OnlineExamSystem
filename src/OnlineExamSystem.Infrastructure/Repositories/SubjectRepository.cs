namespace OnlineExamSystem.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

/// <summary>
/// Subject repository implementation
/// </summary>
public class SubjectRepository : ISubjectRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubjectRepository> _logger;

    public SubjectRepository(ApplicationDbContext context, ILogger<SubjectRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Subject?> GetByIdAsync(long id)
    {
        try
        {
            return await _context.Subjects
                .Include(s => s.Questions)
                .Include(s => s.Exams)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subject by ID {SubjectId}", id);
            throw;
        }
    }

    public async Task<Subject?> GetByCodeAsync(string code)
    {
        try
        {
            return await _context.Subjects
                .FirstOrDefaultAsync(s => s.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subject by code {Code}", code);
            throw;
        }
    }

    public async Task<(List<Subject> Subjects, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Subjects.OrderBy(s => s.Name);

            var totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var subjects = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (subjects, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all subjects");
            throw;
        }
    }

    public async Task<List<Subject>> SearchAsync(string searchTerm)
    {
        try
        {
            var lowerSearchTerm = searchTerm.ToLower();

            return await _context.Subjects
                .Where(s => 
                    s.Name.ToLower().Contains(lowerSearchTerm) ||
                    s.Code.ToLower().Contains(lowerSearchTerm))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching subjects with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<Subject> CreateAsync(Subject subject)
    {
        try
        {
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Subject created with ID {SubjectId}", subject.Id);
            return subject;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subject");
            throw;
        }
    }

    public async Task<Subject> UpdateAsync(Subject subject)
    {
        try
        {
            _context.Subjects.Update(subject);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Subject updated with ID {SubjectId}", subject.Id);
            return subject;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject {SubjectId}", subject.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(long id)
    {
        try
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                _logger.LogWarning("Subject not found for deletion: {SubjectId}", id);
                return false;
            }

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Subject deleted with ID {SubjectId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject {SubjectId}", id);
            throw;
        }
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeSubjectId = null)
    {
        try
        {
            var query = _context.Subjects.Where(s => s.Code == code);
            
            if (excludeSubjectId.HasValue)
            {
                query = query.Where(s => s.Id != excludeSubjectId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if subject code exists: {Code}", code);
            throw;
        }
    }
}
