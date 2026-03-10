namespace OnlineExamSystem.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Data;

/// <summary>
/// Class repository implementation with CRUD operations
/// </summary>
public class ClassRepository : IClassRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClassRepository> _logger;

    public ClassRepository(ApplicationDbContext context, ILogger<ClassRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Class?> GetByIdAsync(long id)
    {
        try
        {
            return await _context.Classes
                .Include(c => c.School)
                .Include(c => c.ClassStudents)
                .Include(c => c.ClassTeachers)
                .Include(c => c.HomeroomTeacher)
                    .ThenInclude(t => t!.User)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting class by ID {ClassId}", id);
            throw;
        }
    }

    public async Task<Class?> GetByCodeAsync(string code)
    {
        try
        {
            return await _context.Classes
                .Include(c => c.School)
                .FirstOrDefaultAsync(c => c.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting class by code {Code}", code);
            throw;
        }
    }

    public async Task<(List<Class> Classes, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Classes
                .Include(c => c.School)
                .Include(c => c.ClassStudents)
                .Include(c => c.ClassTeachers)
                .Include(c => c.HomeroomTeacher)
                    .ThenInclude(t => t!.User)
                .OrderByDescending(c => c.Grade)
                .ThenBy(c => c.Name);

            var totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var classes = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (classes, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all classes (page: {Page}, size: {PageSize})", page, pageSize);
            throw;
        }
    }

    public async Task<(List<Class> Classes, int TotalCount)> GetBySchoolAsync(long schoolId, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Classes
                .Include(c => c.School)
                .Where(c => c.SchoolId == schoolId)
                .OrderByDescending(c => c.Grade)
                .ThenBy(c => c.Name);

            var totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var classes = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (classes, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting classes by school {SchoolId}", schoolId);
            throw;
        }
    }

    public async Task<(List<Class> Classes, int TotalCount)> GetByGradeAsync(int grade, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Classes
                .Include(c => c.School)
                .Where(c => c.Grade == grade)
                .OrderBy(c => c.Name);

            var totalCount = await query.CountAsync();
            var skip = (page - 1) * pageSize;

            var classes = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (classes, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting classes by grade {Grade}", grade);
            throw;
        }
    }

    public async Task<List<Class>> SearchAsync(string searchTerm)
    {
        try
        {
            var lowerSearchTerm = searchTerm.ToLower();

            return await _context.Classes
                .Include(c => c.School)
                .Include(c => c.ClassStudents)
                .Include(c => c.ClassTeachers)
                .Include(c => c.HomeroomTeacher)
                    .ThenInclude(t => t!.User)
                .Where(c => 
                    c.Name.ToLower().Contains(lowerSearchTerm) ||
                    c.Code.ToLower().Contains(lowerSearchTerm))
                .OrderByDescending(c => c.Grade)
                .ThenBy(c => c.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching classes with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<Class> CreateAsync(Class @class)
    {
        try
        {
            _context.Classes.Add(@class);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Class created with ID {ClassId}", @class.Id);
            return @class;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating class");
            throw;
        }
    }

    public async Task<Class> UpdateAsync(Class @class)
    {
        try
        {
            _context.Classes.Update(@class);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Class updated with ID {ClassId}", @class.Id);
            return @class;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating class {ClassId}", @class.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(long id)
    {
        try
        {
            var @class = await _context.Classes.FindAsync(id);
            if (@class == null)
            {
                _logger.LogWarning("Class not found for deletion: {ClassId}", id);
                return false;
            }

            _context.Classes.Remove(@class);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Class deleted with ID {ClassId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting class {ClassId}", id);
            throw;
        }
    }

    public async Task<bool> CodeExistsAsync(string code, long? excludeClassId = null)
    {
        try
        {
            var query = _context.Classes.Where(c => c.Code == code);
            
            if (excludeClassId.HasValue)
            {
                query = query.Where(c => c.Id != excludeClassId.Value);
            }

            return await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if class code exists: {Code}", code);
            throw;
        }
    }

    public async Task<List<ClassStudent>> GetClassStudentsAsync(long classId)
    {
        try
        {
            return await _context.ClassStudents
                .Where(cs => cs.ClassId == classId)
                .Include(cs => cs.Student).ThenInclude(s => s.User)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students for class {ClassId}", classId);
            throw;
        }
    }

    public async Task<bool> AddStudentToClassAsync(long classId, long studentId)
    {
        try
        {
            var classStudent = new ClassStudent
            {
                ClassId = classId,
                StudentId = studentId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.ClassStudents.Add(classStudent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Student {StudentId} added to class {ClassId}", studentId, classId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding student {StudentId} to class {ClassId}", studentId, classId);
            throw;
        }
    }

    public async Task<bool> RemoveStudentFromClassAsync(long classId, long studentId)
    {
        try
        {
            var classStudent = await _context.ClassStudents
                .FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);

            if (classStudent == null)
            {
                return false;
            }

            _context.ClassStudents.Remove(classStudent);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Student {StudentId} removed from class {ClassId}", studentId, classId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing student {StudentId} from class {ClassId}", studentId, classId);
            throw;
        }
    }

    public async Task<bool> StudentEnrolledInClassAsync(long classId, long studentId)
    {
        try
        {
            return await _context.ClassStudents
                .AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if student {StudentId} is enrolled in class {ClassId}", studentId, classId);
            throw;
        }
    }
}
