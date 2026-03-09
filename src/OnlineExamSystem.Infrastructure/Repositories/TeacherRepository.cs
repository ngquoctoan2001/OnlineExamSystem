namespace OnlineExamSystem.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.Repositories;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

/// <summary>
/// Teacher repository implementation with CRUD operations
/// </summary>
public class TeacherRepository : ITeacherRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TeacherRepository> _logger;

    public TeacherRepository(ApplicationDbContext context, ILogger<TeacherRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Teacher?> GetByIdAsync(long id)
    {
        try
        {
            return await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.ClassTeachers)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teacher by ID {TeacherId}", id);
            throw;
        }
    }

    public async Task<Teacher?> GetByUserIdAsync(long userId)
    {
        try
        {
            return await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teacher by User ID {UserId}", userId);
            throw;
        }
    }

    public async Task<Teacher?> GetByEmployeeIdAsync(string employeeId)
    {
        try
        {
            return await _context.Teachers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.EmployeeId == employeeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting teacher by Employee ID {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<(List<Teacher> Teachers, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Teachers
                .Include(t => t.User)
                .OrderByDescending(t => t.User.CreatedAt);

            var totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var teachers = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (teachers, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all teachers (page: {Page}, size: {PageSize})", page, pageSize);
            throw;
        }
    }

    public async Task<List<Teacher>> SearchAsync(string searchTerm)
    {
        try
        {
            var lowerSearchTerm = searchTerm.ToLower();

            return await _context.Teachers
                .Include(t => t.User)
                .Where(t => 
                    t.User.FullName.ToLower().Contains(lowerSearchTerm) ||
                    t.EmployeeId.ToLower().Contains(lowerSearchTerm) ||
                    t.User.Username.ToLower().Contains(lowerSearchTerm))
                .OrderByDescending(t => t.User.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching teachers with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<List<ClassTeacher>> GetTeacherClassesAsync(long teacherId)
    {
        try
        {
            return await _context.ClassTeachers
                .Where(ct => ct.TeacherId == teacherId)
                .Include(ct => ct.Class)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting classes for teacher {TeacherId}", teacherId);
            throw;
        }
    }

    public async Task<Teacher> CreateAsync(Teacher teacher)
    {
        try
        {
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Teacher created with ID {TeacherId}", teacher.Id);
            return teacher;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating teacher");
            throw;
        }
    }

    public async Task<Teacher> UpdateAsync(Teacher teacher)
    {
        try
        {
            teacher.User!.UpdatedAt = DateTime.UtcNow;
            _context.Teachers.Update(teacher);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Teacher updated with ID {TeacherId}", teacher.Id);
            return teacher;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating teacher {TeacherId}", teacher.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(long id)
    {
        try
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher not found for deletion: {TeacherId}", id);
                return false;
            }

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Teacher deleted with ID {TeacherId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting teacher {TeacherId}", id);
            throw;
        }
    }

    public async Task<bool> EmployeeIdExistsAsync(string employeeId, long? excludeTeacherId = null)
    {
        try
        {
            var query = _context.Teachers.Where(t => t.EmployeeId == employeeId);
            
            if (excludeTeacherId.HasValue)
            {
                query = query.Where(t => t.Id != excludeTeacherId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if employee ID exists: {EmployeeId}", employeeId);
            throw;
        }
    }
}
