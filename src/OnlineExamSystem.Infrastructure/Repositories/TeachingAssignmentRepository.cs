namespace OnlineExamSystem.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

/// <summary>
/// Implements ITeachingAssignmentRepository for managing teacher-class-subject assignments
/// </summary>
public class TeachingAssignmentRepository : ITeachingAssignmentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TeachingAssignmentRepository> _logger;

    public TeachingAssignmentRepository(ApplicationDbContext context, ILogger<TeachingAssignmentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get teaching assignment by ID
    /// </summary>
    public async Task<ClassTeacher?> GetByIdAsync(long id)
    {
        try
        {
            var assignment = await _context.ClassTeachers
                .Include(ct => ct.Class)
                .Include(ct => ct.Teacher)
                    .ThenInclude(t => t.User)
                .Include(ct => ct.Subject)
                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (assignment == null)
            {
                _logger.LogWarning("Teaching assignment not found: {Id}", id);
            }

            return assignment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving teaching assignment: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Get all teaching assignments with pagination
    /// </summary>
    public async Task<(List<ClassTeacher> Assignments, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.ClassTeachers
                .Include(ct => ct.Class)
                .Include(ct => ct.Teacher)
                    .ThenInclude(t => t.User)
                .Include(ct => ct.Subject)
                .OrderByDescending(ct => ct.CreatedDate);

            var totalCount = await query.CountAsync();
            
            var assignments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} teaching assignments, page {Page}", assignments.Count, page);
            return (assignments, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving teaching assignments");
            throw;
        }
    }

    /// <summary>
    /// Get all teaching assignments for a specific class
    /// </summary>
    public async Task<List<ClassTeacher>> GetByClassAsync(long classId)
    {
        try
        {
            var assignments = await _context.ClassTeachers
                .Include(ct => ct.Teacher)
                    .ThenInclude(t => t.User)
                .Include(ct => ct.Subject)
                .Where(ct => ct.ClassId == classId)
                .OrderBy(ct => ct.Subject!.Name)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} assignments for class: {ClassId}", assignments.Count, classId);
            return assignments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignments for class: {ClassId}", classId);
            throw;
        }
    }

    /// <summary>
    /// Get all teaching assignments for a specific teacher
    /// </summary>
    public async Task<List<ClassTeacher>> GetByTeacherAsync(long teacherId)
    {
        try
        {
            var assignments = await _context.ClassTeachers
                .Include(ct => ct.Class)
                .Include(ct => ct.Subject)
                .Where(ct => ct.TeacherId == teacherId)
                .OrderBy(ct => ct.Class!.Name)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} assignments for teacher: {TeacherId}", assignments.Count, teacherId);
            return assignments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignments for teacher: {TeacherId}", teacherId);
            throw;
        }
    }

    /// <summary>
    /// Get all teaching assignments for a specific subject
    /// </summary>
    public async Task<List<ClassTeacher>> GetBySubjectAsync(long subjectId)
    {
        try
        {
            var assignments = await _context.ClassTeachers
                .Include(ct => ct.Class)
                .Include(ct => ct.Teacher)
                    .ThenInclude(t => t.User)
                .Where(ct => ct.SubjectId == subjectId)
                .OrderBy(ct => ct.Class!.Name)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} assignments for subject: {SubjectId}", assignments.Count, subjectId);
            return assignments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignments for subject: {SubjectId}", subjectId);
            throw;
        }
    }

    /// <summary>
    /// Get teaching assignment by class, teacher, and subject
    /// </summary>
    public async Task<ClassTeacher?> GetByClassTeacherSubjectAsync(long classId, long teacherId, long subjectId)
    {
        try
        {
            var assignment = await _context.ClassTeachers
                .Include(ct => ct.Class)
                .Include(ct => ct.Teacher)
                    .ThenInclude(t => t.User)
                .Include(ct => ct.Subject)
                .FirstOrDefaultAsync(ct => 
                    ct.ClassId == classId && 
                    ct.TeacherId == teacherId && 
                    ct.SubjectId == subjectId);

            return assignment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment: Class={ClassId}, Teacher={TeacherId}, Subject={SubjectId}", 
                classId, teacherId, subjectId);
            throw;
        }
    }

    /// <summary>
    /// Search teaching assignments by class name, teacher name, or subject name
    /// </summary>
    public async Task<List<ClassTeacher>> SearchAsync(string searchTerm)
    {
        try
        {
            var query = _context.ClassTeachers
                .Include(ct => ct.Class)
                .Include(ct => ct.Teacher)
                    .ThenInclude(t => t.User)
                .Include(ct => ct.Subject)
                .AsQueryable();

            var assignments = await query
                .Where(ct =>
                    ct.Class!.Name.Contains(searchTerm) ||
                    ct.Teacher!.User.FullName.Contains(searchTerm) ||
                    ct.Subject!.Name.Contains(searchTerm) ||
                    ct.Subject!.Code.Contains(searchTerm))
                .ToListAsync();

            _logger.LogInformation("Search found {Count} teaching assignments for term: {SearchTerm}", 
                assignments.Count, searchTerm);
            return assignments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching assignments with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    /// <summary>
    /// Create new teaching assignment
    /// </summary>
    public async Task<ClassTeacher> CreateAsync(ClassTeacher assignment)
    {
        try
        {
            assignment.CreatedDate = DateTime.UtcNow;
            assignment.UpdatedDate = DateTime.UtcNow;

            _context.ClassTeachers.Add(assignment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Teaching assignment created: Class={ClassId}, Teacher={TeacherId}, Subject={SubjectId}", 
                assignment.ClassId, assignment.TeacherId, assignment.SubjectId);
            
            return assignment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating teaching assignment");
            throw;
        }
    }

    /// <summary>
    /// Update teaching assignment
    /// </summary>
    public async Task<ClassTeacher> UpdateAsync(ClassTeacher assignment)
    {
        try
        {
            assignment.UpdatedDate = DateTime.UtcNow;

            _context.ClassTeachers.Update(assignment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Teaching assignment updated: {Id}", assignment.Id);
            return assignment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating teaching assignment: {Id}", assignment.Id);
            throw;
        }
    }

    /// <summary>
    /// Delete teaching assignment
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        try
        {
            var assignment = await _context.ClassTeachers.FindAsync(id);
            if (assignment == null)
            {
                _logger.LogWarning("Teaching assignment not found for deletion: {Id}", id);
                return false;
            }

            _context.ClassTeachers.Remove(assignment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Teaching assignment deleted: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting teaching assignment: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Check if a teaching assignment already exists
    /// </summary>
    public async Task<bool> AssignmentExistsAsync(long classId, long teacherId, long subjectId, long? excludeId = null)
    {
        try
        {
            var query = _context.ClassTeachers
                .Where(ct => ct.ClassId == classId && ct.TeacherId == teacherId && ct.SubjectId == subjectId);

            if (excludeId.HasValue)
            {
                query = query.Where(ct => ct.Id != excludeId.Value);
            }

            var exists = await query.AnyAsync();
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking assignment existence");
            throw;
        }
    }
}
