namespace OnlineExamSystem.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

/// <summary>
/// Student repository implementation with CRUD operations
/// </summary>
public class StudentRepository : IStudentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentRepository> _logger;

    public StudentRepository(ApplicationDbContext context, ILogger<StudentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Student?> GetByIdAsync(long id)
    {
        try
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.ClassStudents)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student by ID {StudentId}", id);
            throw;
        }
    }

    public async Task<Student?> GetByUserIdAsync(long userId)
    {
        try
        {
            return await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student by User ID {UserId}", userId);
            throw;
        }
    }

    public async Task<Student?> GetByStudentCodeAsync(string studentCode)
    {
        try
        {
            return await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentCode == studentCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student by Student Code {StudentCode}", studentCode);
            throw;
        }
    }

    public async Task<(List<Student> Students, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Students
                .Include(s => s.User)
                .OrderByDescending(s => s.User.CreatedAt);

            var totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var students = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (students, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all students (page: {Page}, size: {PageSize})", page, pageSize);
            throw;
        }
    }

    public async Task<List<Student>> SearchAsync(string searchTerm)
    {
        try
        {
            var lowerSearchTerm = searchTerm.ToLower();

            return await _context.Students
                .Include(s => s.User)
                .Where(s => 
                    s.User.FullName.ToLower().Contains(lowerSearchTerm) ||
                    s.StudentCode.ToLower().Contains(lowerSearchTerm) ||
                    s.User.Username.ToLower().Contains(lowerSearchTerm) ||
                    s.RollNumber.ToLower().Contains(lowerSearchTerm))
                .OrderByDescending(s => s.User.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching students with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<List<ClassStudent>> GetStudentClassesAsync(long studentId)
    {
        try
        {
            return await _context.ClassStudents
                .Where(cs => cs.StudentId == studentId)
                .Include(cs => cs.Class)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting classes for student {StudentId}", studentId);
            throw;
        }
    }

    public async Task<Student> CreateAsync(Student student)
    {
        try
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Student created with ID {StudentId}", student.Id);
            return student;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            throw;
        }
    }

    public async Task<Student> UpdateAsync(Student student)
    {
        try
        {
            student.User!.UpdatedAt = DateTime.UtcNow;
            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Student updated with ID {StudentId}", student.Id);
            return student;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student {StudentId}", student.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(long id)
    {
        try
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                _logger.LogWarning("Student not found for deletion: {StudentId}", id);
                return false;
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Student deleted with ID {StudentId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student {StudentId}", id);
            throw;
        }
    }

    public async Task<bool> StudentCodeExistsAsync(string studentCode, long? excludeStudentId = null)
    {
        try
        {
            var query = _context.Students.Where(s => s.StudentCode == studentCode);
            
            if (excludeStudentId.HasValue)
            {
                query = query.Where(s => s.Id != excludeStudentId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if student code exists: {StudentCode}", studentCode);
            throw;
        }
    }
}
