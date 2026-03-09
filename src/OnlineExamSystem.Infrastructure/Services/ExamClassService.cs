using Microsoft.Extensions.Logging;
using OnlineExamSystem.Application.DTOs;
using OnlineExamSystem.Application.Services;
using OnlineExamSystem.Domain.Entities;
using OnlineExamSystem.Infrastructure.Repositories;

namespace OnlineExamSystem.Infrastructure.Services;

/// <summary>
/// Exam-Class assignment service implementation
/// </summary>
public class ExamClassService : IExamClassService
{
    private readonly IExamClassRepository _examClassRepository;
    private readonly IExamRepository _examRepository;
    private readonly IClassRepository _classRepository;
    private readonly ILogger<ExamClassService> _logger;

    public ExamClassService(
        IExamClassRepository examClassRepository,
        IExamRepository examRepository,
        IClassRepository classRepository,
        ILogger<ExamClassService> logger)
    {
        _examClassRepository = examClassRepository;
        _examRepository = examRepository;
        _classRepository = classRepository;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, ExamClassResponse? Data)> AssignClassToExamAsync(long examId, long classId)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
            {
                _logger.LogWarning("Exam not found: {ExamId}", examId);
                return (false, "Exam not found", null);
            }

            var @class = await _classRepository.GetByIdAsync(classId);
            if (@class == null)
            {
                _logger.LogWarning("Class not found: {ClassId}", classId);
                return (false, "Class not found", null);
            }

            // Check if assignment already exists
            var exists = await _examClassRepository.ExistsAsync(examId, classId);
            if (exists)
            {
                _logger.LogWarning("This class is already assigned to the exam: {ExamId}, {ClassId}", examId, classId);
                return (false, "This class is already assigned to the exam", null);
            }

            var examClass = new ExamClass
            {
                ExamId = examId,
                ClassId = classId,
                AssignedAt = DateTime.UtcNow
            };

            var result = await _examClassRepository.CreateAsync(examClass);

            var response = new ExamClassResponse
            {
                ExamId = result.ExamId,
                ClassId = result.ClassId,
                ExamTitle = exam.Title,
                ClassName = @class.Name,
                AssignedAt = result.AssignedAt
            };

            _logger.LogInformation("Class assigned to exam: {ExamId}, {ClassId}", examId, classId);
            return (true, "Class assigned to exam successfully", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning class to exam: {ExamId}, {ClassId}", examId, classId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, ExamClassesResponse? Data)> GetExamClassesAsync(long examId)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
            {
                _logger.LogWarning("Exam not found: {ExamId}", examId);
                return (false, "Exam not found", null);
            }

            var examClasses = await _examClassRepository.GetExamClassesAsync(examId);

            var response = new ExamClassesResponse
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                Classes = examClasses.Select(ec => new ClassAssignmentResponse
                {
                    ClassId = ec.ClassId,
                    ClassName = ec.Class?.Name ?? string.Empty,
                    StudentCount = ec.Class?.ClassStudents?.Count ?? 0,
                    AssignedAt = ec.AssignedAt
                }).ToList()
            };

            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam classes: {ExamId}", examId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, List<ExamClassResponse>? Data)> GetClassExamsAsync(long classId)
    {
        try
        {
            var @class = await _classRepository.GetByIdAsync(classId);
            if (@class == null)
            {
                _logger.LogWarning("Class not found: {ClassId}", classId);
                return (false, "Class not found", null);
            }

            var classExams = await _examClassRepository.GetClassExamsAsync(classId);

            var responses = classExams.Select(ec => new ExamClassResponse
            {
                ExamId = ec.ExamId,
                ClassId = ec.ClassId,
                ExamTitle = ec.Exam?.Title ?? string.Empty,
                ClassName = @class.Name,
                AssignedAt = ec.AssignedAt
            }).ToList();

            return (true, "Success", responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting class exams: {ClassId}", classId);
            return (false, $"Error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> RemoveClassFromExamAsync(long examId, long classId)
    {
        try
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
            {
                _logger.LogWarning("Exam not found: {ExamId}", examId);
                return (false, "Exam not found");
            }

            var @class = await _classRepository.GetByIdAsync(classId);
            if (@class == null)
            {
                _logger.LogWarning("Class not found: {ClassId}", classId);
                return (false, "Class not found");
            }

            var deleted = await _examClassRepository.DeleteAsync(examId, classId);
            if (!deleted)
            {
                _logger.LogWarning("Assignment not found: {ExamId}, {ClassId}", examId, classId);
                return (false, "Assignment not found");
            }

            _logger.LogInformation("Class removed from exam: {ExamId}, {ClassId}", examId, classId);
            return (true, "Class removed from exam successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing class from exam: {ExamId}, {ClassId}", examId, classId);
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, ExamClassListResponse? Data)> GetAllAssignmentsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var (examClasses, totalCount) = await _examClassRepository.GetAllAsync(page, pageSize);

            var response = new ExamClassListResponse
            {
                Items = examClasses.Select(ec => new ExamClassResponse
                {
                    ExamId = ec.ExamId,
                    ClassId = ec.ClassId,
                    ExamTitle = ec.Exam?.Title ?? string.Empty,
                    ClassName = ec.Class?.Name ?? string.Empty,
                    AssignedAt = ec.AssignedAt
                }).ToList(),
                TotalCount = totalCount
            };

            return (true, "Success", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all assignments");
            return (false, $"Error: {ex.Message}", null);
        }
    }
}
