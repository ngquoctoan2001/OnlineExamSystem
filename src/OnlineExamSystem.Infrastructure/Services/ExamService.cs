namespace OnlineExamSystem.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.Services;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

/// <summary>
/// Exam service implementation with business logic
/// </summary>
public class ExamService : IExamService
{
    private readonly IExamRepository _examRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<ExamService> _logger;

    public ExamService(
        IExamRepository examRepository,
        ITeacherRepository teacherRepository,
        ISubjectRepository subjectRepository,
        ILogger<ExamService> logger)
    {
        _examRepository = examRepository;
        _teacherRepository = teacherRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ExamResponse? Data)> GetExamByIdAsync(long id)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(id);
            
            if (exam == null)
            {
                _logger.LogWarning("Exam not found: {ExamId}", id);
                return (false, "Exam not found", null);
            }

            var response = MapToExamResponse(exam);
            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam {ExamId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamListResponse? Data)> GetAllExamsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (exams, totalCount) = await _examRepository.GetAllAsync(page, pageSize);
            
            var response = new ExamListResponse
            {
                Items = exams.Select(MapToExamResponse).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all exams");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ExamResponse>? Data)> SearchExamsAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return (false, "Search term cannot be empty", null);

            var exams = await _examRepository.SearchAsync(searchTerm);
            var responses = exams.Select(MapToExamResponse).ToList();

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching exams");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ExamResponse>? Data)> GetExamsByTeacherAsync(long teacherId)
    {
        try
        {
            var teacher = await _teacherRepository.GetByIdAsync(teacherId);
            if (teacher == null)
            {
                _logger.LogWarning("Teacher not found: {TeacherId}", teacherId);
                return (false, "Teacher not found", null);
            }

            var exams = await _examRepository.GetByTeacherAsync(teacherId);
            var responses = exams.Select(MapToExamResponse).ToList();

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exams by teacher {TeacherId}", teacherId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ExamResponse>? Data)> GetExamsBySubjectAsync(long subjectId)
    {
        try
        {
            var subject = await _subjectRepository.GetByIdAsync(subjectId);
            if (subject == null)
            {
                _logger.LogWarning("Subject not found: {SubjectId}", subjectId);
                return (false, "Subject not found", null);
            }

            var exams = await _examRepository.GetBySubjectAsync(subjectId);
            var responses = exams.Select(MapToExamResponse).ToList();

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exams by subject {SubjectId}", subjectId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamResponse? Data)> CreateExamAsync(CreateExamRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title))
                return (false, "Exam title is required", null);

            if (request.DurationMinutes <= 0)
                return (false, "Duration must be greater than 0", null);

            if (request.StartTime >= request.EndTime)
                return (false, "Start time must be before end time", null);

            // Check if exam title already exists
            if (await _examRepository.TitleExistsAsync(request.Title))
                return (false, "An exam with this title already exists", null);

            // Verify teacher exists
            var teacher = await _teacherRepository.GetByIdAsync(request.CreatedBy);
            if (teacher == null)
                return (false, "Teacher not found", null);

            // Verify subject exists
            var subject = await _subjectRepository.GetByIdAsync(request.SubjectId);
            if (subject == null)
                return (false, "Subject not found", null);

            var exam = new Exam
            {
                Title = request.Title.Trim(),
                SubjectId = request.SubjectId,
                CreatedBy = request.CreatedBy,
                DurationMinutes = request.DurationMinutes,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Description = request.Description?.Trim() ?? string.Empty,
                Status = "DRAFT"
            };

            var createdExam = await _examRepository.CreateAsync(exam);
            var response = MapToExamResponse(createdExam);

            _logger.LogInformation("Exam created: {ExamId}", createdExam.Id);
            return (true, "Exam created successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exam");
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamResponse? Data)> UpdateExamAsync(long id, UpdateExamRequest request)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(id);
            if (exam == null)
                return (false, "Exam not found", null);

            // Can only update if in DRAFT status
            if (exam.Status != "DRAFT")
                return (false, "Cannot update exam in ACTIVE or CLOSED status", null);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title))
                return (false, "Exam title is required", null);

            if (request.DurationMinutes <= 0)
                return (false, "Duration must be greater than 0", null);

            if (request.StartTime >= request.EndTime)
                return (false, "Start time must be before end time", null);

            // Check if new title exists (excluding current exam)
            if (request.Title.Trim() != exam.Title && await _examRepository.TitleExistsAsync(request.Title, id))
                return (false, "An exam with this title already exists", null);

            // Verify subject exists
            var subject = await _subjectRepository.GetByIdAsync(request.SubjectId);
            if (subject == null)
                return (false, "Subject not found", null);

            exam.Title = request.Title.Trim();
            exam.SubjectId = request.SubjectId;
            exam.DurationMinutes = request.DurationMinutes;
            exam.StartTime = request.StartTime;
            exam.EndTime = request.EndTime;
            exam.Description = request.Description?.Trim() ?? string.Empty;

            var updatedExam = await _examRepository.UpdateAsync(exam);
            var response = MapToExamResponse(updatedExam);

            _logger.LogInformation("Exam updated: {ExamId}", id);
            return (true, "Exam updated successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating exam {ExamId}", id);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteExamAsync(long id)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(id);
            if (exam == null)
                return (false, "Exam not found");

            // Can only delete DRAFT exams
            if (exam.Status != "DRAFT")
                return (false, "Cannot delete exam that is ACTIVE or CLOSED");

            var deleted = await _examRepository.DeleteAsync(id);
            if (!deleted)
                return (false, "Failed to delete exam");

            _logger.LogInformation("Exam deleted: {ExamId}", id);
            return (true, "Exam deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting exam {ExamId}", id);
            return (false, $"Error: {ex.Message}");
        }
    }

    private ExamResponse MapToExamResponse(Exam exam)
    {
        return new ExamResponse
        {
            Id = exam.Id,
            Title = exam.Title,
            SubjectId = exam.SubjectId,
            SubjectName = exam.Subject?.Name ?? string.Empty,
            CreatedBy = exam.CreatedBy,
            DurationMinutes = exam.DurationMinutes,
            StartTime = exam.StartTime,
            EndTime = exam.EndTime,
            Description = exam.Description,
            Status = exam.Status,
            CreatedAt = exam.CreatedAt
        };
    }
}
